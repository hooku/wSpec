//*****************************************************************************
//
// Copyright (C) 2014 Texas Instruments Incorporated - http://www.ti.com/ 
// 
// 
//  Redistribution and use in source and binary forms, with or without 
//  modification, are permitted provided that the following conditions 
//  are met:
//
//    Redistributions of source code must retain the above copyright 
//    notice, this list of conditions and the following disclaimer.
//
//    Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the 
//    documentation and/or other materials provided with the   
//    distribution.
//
//    Neither the name of Texas Instruments Incorporated nor the names of
//    its contributors may be used to endorse or promote products derived
//    from this software without specific prior written permission.
//
//  THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS 
//  "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT 
//  LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
//  A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT 
//  OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, 
//  SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT 
//  LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
//  DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
//  THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT 
//  (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE 
//  OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
//
//*****************************************************************************


//*****************************************************************************
//
// Application Name     -   Wifi Audio Application
// Application Overview -   This is a sample application demonstrating 
//                          Bi-directional audio transfer.Audio is streamed
//                          from one LP and rendered on another LP over wifi. 
//
// Application Details  -
// http://processors.wiki.ti.com/index.php/CC32xx_Wifi_Audio_Application
// or
// doc\examples\CC32xx Wifi Audio Application.pdf
//
//*****************************************************************************

//****************************************************************************
//
//! \addtogroup wifi_audio_app
//! @{
//
//****************************************************************************
#include <stdlib.h>
#include <string.h>

//SimpleLink includes
#include "simplelink.h"

// free-rtos/ ti_rtos includes
#include "osi.h"

// Hardware & DriverLib library includes.
#include "hw_types.h"
#include "hw_ints.h"
#include "hw_memmap.h"
#include "hw_common_reg.h"
#include "interrupt.h"
#include "i2s.h"
#include "udma.h"
#include "gpio.h"
#include "gpio_if.h"
#include "prcm.h"
#include "rom.h"
#include "rom_map.h"
#include "pin.h"
#include "utils.h"

//Common interface includes
#include "common.h"
#include "udma_if.h"
#include "uart_if.h"
#include "i2c_if.h"



//App include
#include "pinmux.h"
#include "network.h"
#include "circ_buff.h"
#include "control.h"
#include "audioCodec.h"
#include "i2s_if.h"
#include "pcm_handler.h"


#define APPLICATION_VERSION     "1.1.1"
#define OSI_STACK_SIZE          1024

#ifdef WSPEC
#define PAD_MODE_MASK           0x0000000F
#define PAD_STRENGTH_MASK       0x000000E0
#define PAD_TYPE_MASK           0x00000310

#define REG_PAD_CONFIG_26       0x4402E108      // antenna sel1
#define REG_PAD_CONFIG_27       0x4402E10C      // antenna sel2
#endif // WSPEC
//*****************************************************************************
//                 GLOBAL VARIABLES -- Start
//*****************************************************************************
tCircularBuffer *pRecordBuffer;
tCircularBuffer *pPlayBuffer;
tUDPSocket g_UdpSock;
OsiTaskHandle g_SpeakerTask = NULL ;
OsiTaskHandle g_MicTask = NULL ;
OsiTaskHandle g_NetworkTask = NULL ;

unsigned char g_loopback=1;

#if defined(ccs)
extern void (* const g_pfnVectors[])(void);
#endif
#if defined(ewarm)
extern uVectorEntry __vector_table;
#endif
#ifdef WSPEC
extern unsigned int g_uiLED1Port, g_uiLED2Port, g_uiLED3Port;
extern unsigned char g_ucLED1Pin, g_ucLED2Pin, g_ucLED3Pin;
#endif // WSPEC
//*****************************************************************************
//                 GLOBAL VARIABLES -- End
//*****************************************************************************


//******************************************************************************
//                    FUNCTION DECLARATIONS
//******************************************************************************
extern void Speaker( void *pvParameters );
extern void Microphone( void *pvParameters );
extern void Network( void *pvParameters );

//*****************************************************************************
//
//! Application defined hook (or callback) function - the tick hook.
//! The tick interrupt can optionally call this
//!
//! \param  none
//! 
//! \return none
//!
//*****************************************************************************
void
vApplicationTickHook( void )
{
}

//*****************************************************************************
//
//! Application defined hook (or callback) function - assert
//!
//! \param  none
//! 
//! \return none
//!
//*****************************************************************************
void
vAssertCalled( const char *pcFile, unsigned long ulLine )
{
    while(1)
    {

    }
}

//*****************************************************************************
//
//! Application defined idle task hook
//! 
//! \param  none
//! 
//! \return none
//!
//*****************************************************************************
void
vApplicationIdleHook( void )
{

}

//*****************************************************************************
//
//! Application provided stack overflow hook function.
//!
//! \param  handle of the offending task
//! \param  name  of the offending task
//! 
//! \return none
//!
//*****************************************************************************
void
vApplicationStackOverflowHook( OsiTaskHandle *pxTask, signed char *pcTaskName)
{
    ( void ) pxTask;
    ( void ) pcTaskName;

    for( ;; );
}

void vApplicationMallocFailedHook()
{
    while(1)
    {
        // Infinite loop;
    }
}

//*****************************************************************************
//
//! Board Initialization & Configuration
//!
//! \param  None
//!
//! \return None
//
//*****************************************************************************
void
BoardInit(void)
{
    /* In case of TI-RTOS vector table is initialize by OS itself */
#ifndef USE_TIRTOS
    //
    // Set vector table base
    //
#if defined(ccs)
    MAP_IntVTableBaseSet((unsigned long)&g_pfnVectors[0]);
#endif
#if defined(ewarm)
    MAP_IntVTableBaseSet((unsigned long)&__vector_table);
#endif
#endif
    //
    // Enable Processor
    //
    MAP_IntMasterEnable();
    MAP_IntEnable(FAULT_SYSTICK);

    PRCMCC3200MCUInit();
}
#ifdef WSPEC
static void SetAntennaSelectionGPIOs(void)
{

    MAP_PRCMPeripheralClkEnable(PRCM_GPIOA3, PRCM_RUN_MODE_CLK);
    MAP_GPIODirModeSet(GPIOA3_BASE,0xC,GPIO_DIR_MODE_OUT);
    
    //
    // Configure PIN_29 for GPIOOutput
    //    
    HWREG(REG_PAD_CONFIG_26) = ((HWREG(REG_PAD_CONFIG_26) & ~(PAD_STRENGTH_MASK 
                                | PAD_TYPE_MASK)) | (0x00000020 | 0x00000000 ));
    
    //
    // Set the mode.
    //
    HWREG(REG_PAD_CONFIG_26) = (((HWREG(REG_PAD_CONFIG_26) & ~PAD_MODE_MASK) |  
                                  0x00000000) & ~(3<<10));
    
    //
    // Set the direction
    //
    HWREG(REG_PAD_CONFIG_26) = ((HWREG(REG_PAD_CONFIG_26) & ~0xC00) | 0x00000800);
    
    
     //
    // Configure PIN_30 for GPIOOutput
    //
    HWREG(REG_PAD_CONFIG_27) = ((HWREG(REG_PAD_CONFIG_27) & ~(PAD_STRENGTH_MASK
                                | PAD_TYPE_MASK)) | (0x00000020 | 0x00000000 ));
    
    //
    // Set the mode.
    //
    HWREG(REG_PAD_CONFIG_27) = (((HWREG(REG_PAD_CONFIG_27) & ~PAD_MODE_MASK) |  
                                  0x00000000) & ~(3<<10));

    //
    // Set the direction
    //
    HWREG(REG_PAD_CONFIG_26) = ((HWREG(REG_PAD_CONFIG_27) & ~0xC00) | 0x00000800);
    
    
    // set gpio 28
    PinTypeGPIO(PIN_18, PIN_MODE_0, false);
    GPIODirModeSet(GPIOA3_BASE, 0x10, GPIO_DIR_MODE_OUT);
}
#endif // WSPEC
//******************************************************************************
//                            MAIN FUNCTION
//******************************************************************************
int main()
{   
    long lRetVal = -1;
    unsigned char	RecordPlay;

    BoardInit();
#ifdef WSPEC
    // fix soft reset bug:
    PRCMPeripheralReset(PRCM_I2S);
    PRCMPeripheralReset(PRCM_UDMA);
#endif // WSPEC

    //
    // Pinmux Configuration
    //
    PinMuxConfig();

    //
    // Initialising the UART terminal
    //
    InitTerm();

#ifdef TESTING
#else // TESTING
    //
    // Initialising the I2C Interface
    //    
#ifdef WSPEC
    lRetVal = I2C_IF_Open(I2C_MASTER_MODE_STD);
#else // WSPEC
    lRetVal = I2C_IF_Open(1);
#endif // WSPEC
    if(lRetVal < 0)
    {
        ERR_PRINT(lRetVal);
        LOOP_FOREVER();
    }

    RecordPlay = I2S_MODE_RX_TX;
    g_loopback = 1;


    //
    // Create RX and TX Buffer
    //
    if(RecordPlay & I2S_MODE_TX)
    {
        pRecordBuffer = CreateCircularBuffer(RECORD_BUFFER_SIZE);
        if(pRecordBuffer == NULL)
        {
            UART_PRINT("Unable to Allocate Memory for Tx Buffer\n\r");
            LOOP_FOREVER();
        }
#ifdef WSPEC
        memset(pRecordBuffer->pucBufferStartPtr, 0, RECORD_BUFFER_SIZE);
#endif // WSPEC
    }

    /* Play */
    if(RecordPlay == I2S_MODE_RX_TX)
    {
        pPlayBuffer = CreateCircularBuffer(PLAY_BUFFER_SIZE);
        if(pPlayBuffer == NULL)
        {
            UART_PRINT("Unable to Allocate Memory for Rx Buffer\n\r");
            LOOP_FOREVER();
        }
#ifdef WSPEC
        memset(pPlayBuffer->pucBufferStartPtr, 0, PLAY_BUFFER_SIZE);
#endif // WSPEC
    }


    //
    // Configure Audio Codec
    //     
    AudioCodecReset(AUDIO_CODEC_TI_3254, NULL);
#ifdef WSPEC
    AudioCodecConfig(AUDIO_CODEC_TI_3254, AUDIO_CODEC_16_BIT, 16000,
                      AUDIO_CODEC_STEREO, AUDIO_CODEC_SPEAKER_ALL,
                      AUDIO_CODEC_MIC_ONBOARD);
#else // WSPEC
    AudioCodecConfig(AUDIO_CODEC_TI_3254, AUDIO_CODEC_16_BIT, 16000,
                      AUDIO_CODEC_STEREO, AUDIO_CODEC_SPEAKER_ALL,
                      AUDIO_CODEC_MIC_ALL);
#endif // WSPEC

    AudioCodecSpeakerVolCtrl(AUDIO_CODEC_TI_3254, AUDIO_CODEC_SPEAKER_ALL, 50);
    AudioCodecMicVolCtrl(AUDIO_CODEC_TI_3254, AUDIO_CODEC_SPEAKER_ALL, 65);

#ifdef WSPEC
    SetAntennaSelectionGPIOs();
    GPIO_IF_LedConfigure(LED1|LED2|LED3);
    
    GPIO_IF_LedOff(MCU_ORANGE_LED_GPIO);
    GPIO_IF_LedOff(MCU_GREEN_LED_GPIO);
    GPIO_IF_LedOff(MCU_RED_LED_GPIO);
    
    MAP_UtilsDelay(1000);
    
    GPIO_IF_LedOn(MCU_ORANGE_LED_GPIO);
    GPIO_IF_LedOn(MCU_GREEN_LED_GPIO);
    GPIO_IF_LedOn(MCU_RED_LED_GPIO);
#else // WSPEC
    GPIO_IF_LedConfigure(LED2|LED3);
#endif // WSPEC    
    
    GPIO_IF_LedOff(MCU_RED_LED_GPIO);
    GPIO_IF_LedOff(MCU_GREEN_LED_GPIO);    
    
    //
    // Configure PIN_01 for GPIOOutput
    //
    //MAP_PinTypeGPIO(PIN_01, PIN_MODE_0, false);
    // MAP_GPIODirModeSet(GPIOA1_BASE, 0x4, GPIO_DIR_MODE_OUT);

    //
    // Configure PIN_02 for GPIOOutput
    //
    //MAP_PinTypeGPIO(PIN_02, PIN_MODE_0, false);
    // MAP_GPIODirModeSet(GPIOA1_BASE, 0x8, GPIO_DIR_MODE_OUT);


    //Turning off Green,Orange LED after i2c writes completed - First Time
    GPIO_IF_LedOff(MCU_GREEN_LED_GPIO);
    GPIO_IF_LedOff(MCU_ORANGE_LED_GPIO);

    //
    // Initialize the Audio(I2S) Module
    //    

    AudioInit();
#endif // TESTING

    //
    // Initialize the DMA Module
    //    
    UDMAInit();
    if(RecordPlay == I2S_MODE_RX_TX)
    {
        UDMAChannelSelect(UDMA_CH5_I2S_TX, NULL);
        SetupPingPongDMATransferRx(pPlayBuffer);
    }
    if(RecordPlay & I2S_MODE_TX)
    {
        UDMAChannelSelect(UDMA_CH4_I2S_RX, NULL);
        SetupPingPongDMATransferTx(pRecordBuffer);
    }

    //
    // Setup the Audio In/Out
    //     
    lRetVal = AudioSetupDMAMode(DMAPingPongCompleteAppCB_opt, \
                                 CB_EVENT_CONFIG_SZ, RecordPlay);
    if(lRetVal < 0)
    {
        ERR_PRINT(lRetVal);
        LOOP_FOREVER();
    }    
#ifdef WSPEC
    AudioCaptureRendererConfigure(AUDIO_CODEC_16_BIT, 16000, AUDIO_CODEC_STEREO, RecordPlay, 1);
#else // WSPEC
    AudioCaptureRendererConfigure(AUDIO_CODEC_16_BIT, 16000, AUDIO_CODEC_STEREO, RecordPlay, 1);
#endif // WSPEC

    //
    // Start Audio Tx/Rx
    //     
    Audio_Start(RecordPlay);

    //
    // Start the simplelink thread
    //
    lRetVal = VStartSimpleLinkSpawnTask(9);  
    if(lRetVal < 0)
    {
        ERR_PRINT(lRetVal);
        LOOP_FOREVER();
    }

#ifdef WSPEC
    UART_PRINT("Audio Init done\r\n");
#endif // WSPEC
    
    //
    // Start the Network Task
    //
    lRetVal = osi_TaskCreate( Network, (signed char*)"NetworkTask",\
                               OSI_STACK_SIZE, NULL,
                               1, &g_NetworkTask );
    if(lRetVal < 0)
    {
        ERR_PRINT(lRetVal);
        LOOP_FOREVER();
    }

    //
    // Start the Control Task
    //
    lRetVal = ControlTaskCreate();
    if(lRetVal < 0)
    {
        ERR_PRINT(lRetVal);
        LOOP_FOREVER();
    }    

#if 0
    //
    // Start the Microphone Task
    //
    lRetVal = osi_TaskCreate( Microphone,(signed char*)"MicroPhone", \
                               OSI_STACK_SIZE, NULL,
                               1, &g_MicTask );
    if(lRetVal < 0)
    {
        ERR_PRINT(lRetVal);
        LOOP_FOREVER();
    }
#endif

    //
    // Start the Speaker Task
    //
    lRetVal = osi_TaskCreate( Speaker, (signed char*)"Speaker",OSI_STACK_SIZE, \
                               NULL, 1, &g_SpeakerTask );
    if(lRetVal < 0)
    {
        ERR_PRINT(lRetVal);
        LOOP_FOREVER();
    }

    //
    // Start the task scheduler
    //
    osi_start();
}

//*****************************************************************************
//
// Close the Doxygen group.
//! @}
//
//*****************************************************************************
