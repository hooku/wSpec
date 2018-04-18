using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Speech.Synthesis;
using System.Media;
using System.IO;

namespace wSpecXY
{
    class sxy_ms
    {
        static SpeechSynthesizer synth = new SpeechSynthesizer();

        private static void log(LOG_TYPE log_type, string text)
        {
            logger.log(log_type, "[msms] " + text);
        }

        private static void log(string text)
        {
            log(LOG_TYPE.LOG_INFO, text);
        }

        public static IEnumerable<string> get_voice()
        {
            if (synth.GetInstalledVoices().Count == 0)
            {
                yield break;
            }

            foreach (InstalledVoice voice in synth.GetInstalledVoices())
            {
                VoiceInfo info = voice.VoiceInfo;

                string voice_item = info.Name;

                yield return voice_item;
            }
        }

        public static void set_voice(string voice_name, int voice_speed)
        {
            log("Vos:" + voice_name + ", Spd:" + voice_speed);

            synth.SelectVoice(voice_name);
            synth.Rate = voice_speed;
        }

        public static void speak(string text)
        {
            SoundPlayer sound_player = new SoundPlayer();

            Byte[] result_byte = exec(text);

            File.WriteAllBytes("tts.wav", result_byte);

            MemoryStream wave_stream = new MemoryStream(result_byte);

            sound_player.Stream = wave_stream;
            sound_player.Play();
        }

        public static Byte[] exec(string text)
        {
            Byte[] result_byte = null;
            MemoryStream wave_stream = new MemoryStream();

            log("Syn:" + text);

            try
            {
                synth.SetOutputToWaveStream(wave_stream);
                synth.Speak(text);
                result_byte = wave_stream.ToArray();

                log("OK");
            }
            catch (Exception ex)
            {
                log(LOG_TYPE.LOG_ERROR, ex.Message);
            }

            return result_byte;
        }
    }
}
