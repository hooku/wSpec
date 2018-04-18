# wSpec
wSpec(wireless Speech ..) is a project to demo the audio functionality on DY-IOT-LPB V1.0 board.

## Architecture
The project is a typical C/S mode arch.

```
Server(PC) <=== AP(WIFI) ===> Client(CC3200)
```

The server application(Windows App) is written in C#. It provide the client the speech recognition/synthesis interface.  
The client side is the CC3200 firmware, which is written in C. The CC3200 board act as the speech recorder & speech speaker.

## Hardware Requirement
1. [CC3200 LaunchPad](http://www.ti.com/tool/cc3200-launchxl)
2. [DY-IOT-LPB V1.0 main board + DY-IOT-LPB Audio Board](http://www.gototi.com)
3. An external speaker/headphone.

## Software Setup
1. IAR Embedded Workbench for ARM
2. [CC3200 SDK 1.1](http://www.ti.com/tool/cc3200sdk) or newer
3. Visual Studio 2010 or newer

## Debug Strategy
* Use TLV3254 Codec's loop-back mode first to test if the on-board MIC works or not.
* Use a DMA loop-back code to test the DY-IOT-LPB board to ensure that both playing and recording path works.
* Use [CCS UniFlash](http://processors.wiki.ti.com/index.php/Category:CCS_UniFlash_Release_Notes_Archive) to update CC3200 to the latest service pack to avoid TI's network processor bug.
* It is suggested to burn the working firmware first, and later on the consequential debugging of server code is easier on PC side.

The client code is a modification version of the original CC3200's wifi_audio_app.

**Known Issue:**
1. Some MIC on the external board of DY-IOT-PB could be damaged out of factory, which means the MIC cannot generate signal at all.
2. On DY-IOT-LPB V1.0, there is no way to reset the codec chip via MCU. Once the codec starts DMA data transfer, the MCU cannot soft reset the codec. A power-down & power-up sequence should be applied to the board once the codec is hang.

_This project was done in [emlab](http://www.emlab.net)_
