using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;

namespace wSpecXY
{
    class sxy_control
    {
        public enum XY_STATE
        {
            XY_IDLE,
            XY_RECV_RTP,
            XY_RECV_RAW,
            XY_BIDU,
            XY_LOCA,
            XY_IBOT,
            XY_SIMI,
            XY_MSMS,
            XY_SEND_RTP,
            XY_SEND_RAW,
        }

        Thread ctl_thread;

        public static XY_STATE xy_state;

        private static byte[] wav_convert_rate(byte[] old_data, int old_rate, int old_channel, int new_rate, int new_channel)
        {
            byte[] new_data = new byte[0];

            // convert audio to 16bit, 16k, stereo:
            MemoryStream wav_src = new MemoryStream(old_data);

            NAudio.Wave.WaveFormat wav_old_fmt = new NAudio.Wave.WaveFormat(old_rate, 16, old_channel);
            NAudio.Wave.WaveFormat wav_new_fmt = new NAudio.Wave.WaveFormat(new_rate, 16, new_channel);

            NAudio.Wave.WaveStream wav_stream = new NAudio.Wave.RawSourceWaveStream(wav_src, wav_old_fmt);

            NAudio.Wave.WaveStream wav_conv = new NAudio.Wave.WaveFormatConversionStream(wav_new_fmt, wav_stream);

            wav_conv.Position = 0;
            while (wav_conv.Position < wav_conv.Length)
            {
                int read_len;
                byte[] read_buffer = new byte[1024];
                read_len = wav_conv.Read(read_buffer, 0, 1024);
                if (read_len > 0)
                {
                    Array.Resize(ref new_data, new_data.Length + read_len);
                    Buffer.BlockCopy(read_buffer, 0, new_data, new_data.Length - read_len, read_len);
                }
                else
                {
                    break;
                }
            }

            return new_data;
        }

        private static void wav_calib(ref byte[] data)
        {
            const byte LEFT_OFFSET = 119;
            const byte RIGHT_OFFSET = 134;
            const int AUDIO_BOOST = 6;

            int l_or_r = 0;

            ushort sample, sample_offset;

            int i_data;
            for (i_data = 0; i_data < data.Length; i_data+=2)
            {
                sample = (ushort)((data[i_data + 1] << 8) + (data[i_data]));

                if (l_or_r == 0)
                { // left:
                    sample_offset = (LEFT_OFFSET << 8);
                    l_or_r = 1;
                }
                else
                {
                    sample_offset = (RIGHT_OFFSET << 8);
                    l_or_r = 0;
                }
                sample -= sample_offset;

                sample *= AUDIO_BOOST;

                if (sample < 0)
                {
                    data[i_data + 1] = (byte)((((sample >> 8) + 0x7F) | 0x80) & 0xFF);
                }
                else
                {
                    data[i_data + 1] = (byte)((sample >> 8) & 0xFF);
                }
                data[i_data] = (byte)(sample & 0xFF);
            }
        }

        private static byte[] wav_16_s_2_m(byte[] data)
        {
            byte[] new_data = new byte[data.Length/2];

            int i_data;
            for (i_data = 0; i_data < new_data.Length; i_data+=2)
            {
                new_data[i_data + 1] = data[i_data*2 + 1];
                new_data[i_data] = data[i_data*2];
            }

            return new_data;
        }

        private static void wav_save(byte[] data, string file_name, int rate, int channel)
        {
            try
            {
                if (File.Exists(file_name))
                {
                    File.Delete(file_name);
                }

                NAudio.Wave.WaveFormat wav_new_fmt = new NAudio.Wave.WaveFormat(rate, 16, channel);
                NAudio.Wave.WaveFileWriter wav_writer = new NAudio.Wave.WaveFileWriter(file_name, wav_new_fmt);

                wav_writer.Write(data, 0, data.Length);
                wav_writer.Close();
            }
            catch (Exception ex)
            {

            }

            return;
        }

        private void control_thread()
        {
            byte[] data_byte = new byte[0];
            string data_string = string.Empty, data_string_new;

            while (true)
            {
                switch (xy_state)
                {
                    case XY_STATE.XY_RECV_RTP:
                    case XY_STATE.XY_RECV_RAW:
                        data_byte = sxy_server.receive_raw();
                        if (data_byte.Length > 0)
                        {
                            wav_calib(ref data_byte);
                            wav_save(data_byte, "dump1.wav", 16000, 2);
                            data_byte = wav_16_s_2_m(data_byte);
                            //data_byte = wav_convert_rate(data_byte, 16000, 2, 16000, 1);
                            wav_save(data_byte, "dump2.wav", 16000, 1);
                            xy_state = XY_STATE.XY_BIDU;
                        }
                        break;
                    case XY_STATE.XY_BIDU:
                        data_string_new = sxy_bidu.exec(data_byte);
                        if (data_string_new.Length > 0)
                        {
                            data_string = data_string_new;
                            xy_state = XY_STATE.XY_LOCA;
                        }
                        else
                        {
                            data_string = "对不起，没听懂，请你再说一遍。";
                            xy_state = XY_STATE.XY_MSMS;
                        }
                        break;
                    case XY_STATE.XY_LOCA:
                        data_string_new = sxy_local.exec(data_string);
                        if (data_string_new.Length > 0)
                        {
                            data_string = data_string_new;
                            xy_state = XY_STATE.XY_MSMS;
                        }
                        else
                        {
                            xy_state = XY_STATE.XY_IBOT;
                        }
                        break;
                    case XY_STATE.XY_IBOT:
                        data_string_new = sxy_ibot.exec(data_string);
                        if (data_string_new.Length > 0)
                        {
                            data_string = data_string_new;
                            xy_state = XY_STATE.XY_MSMS;
                        }
                        else
                        {
                            xy_state = XY_STATE.XY_SIMI;
                        }
                        break;
                    case XY_STATE.XY_SIMI:
                        data_string_new = sxy_ibot.exec(data_string);
                        if (data_string_new.Length > 0)
                        {
                            data_string = data_string_new;
                            xy_state = XY_STATE.XY_MSMS;
                        }
                        else
                        {
                            data_string = "不能回答这个问题。";
                            xy_state = XY_STATE.XY_MSMS;
                        }
                        break;
                    case XY_STATE.XY_MSMS:
                        data_byte = sxy_ms.exec(data_string);
                        // convert speech synthesis to 3200 audible:
                        data_byte = wav_convert_rate(data_byte, 22050, 1, 16000, 2);

                        if (data_byte.Length > 0)
                        {
                            xy_state = XY_STATE.XY_SEND_RAW;
                        }
                        break;
                    case XY_STATE.XY_SEND_RAW:
                    case XY_STATE.XY_SEND_RTP:
                        sxy_server.send_raw(data_byte);

                        xy_state = XY_STATE.XY_RECV_RAW;    // wrap round

                        break;
                    case XY_STATE.XY_IDLE:
                    default:
                        Thread.Sleep(1000);
                        xy_state = XY_STATE.XY_RECV_RAW;
                        break;
                }
            }
        }

        public sxy_control()
        {
            /* create background thread */
            ThreadStart thread_d = new ThreadStart(control_thread);
            ctl_thread = new Thread(thread_d);
            ctl_thread.IsBackground = true;  // auto terminates when app exit
            ctl_thread.Start();              // our worker thread should persist
        }
    }
}
