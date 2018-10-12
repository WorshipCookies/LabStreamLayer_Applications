# ApplicationLSL_Test

This is an application suite that I am currently working on for multimodal data collection experiments. Applications developed in this suite all work on top of the LabStreaming Layer middleware allowing to fully synchronize different devices. This code is currently a work in progress and will be slowly updated as time goes on.

# Making it Run

All these applications are running in x64, so when compiling make sure that you are compiling in x64 (not CPU or x86). All code was developed in Visual Studio 2017, so it should work out of the box in that system. I haven't tested on other compilers. 

It is also important to obtain the LabRecorder from the original LabStreaming Layer software, as all devices will be linked to this software. Once all devices are linked LabRecorder will take care of synchronizing all of the different applications.

# Dependencies

The majority of this software was developed on the .NET Framework 4.7.1., futhermore given that these applications are loading external dlls (i.e. LabStreaming Layer and Bitalino) it is necessary to also download and install the Microsoft Visual VC++ Redistributable 2013 and 2017 (both x86 and x64).

# BitalinoRecorder Application

This application is capable of recording several channels from the Bitalino Device. Currently, it is set to record 3 channels, which was tested on ECG (Electrocardiogram), EDA (Electrodermal Activity) and Respiration. To fully make it work it is necessary to run LabRecorder in parallel and link this app to it.

# IntelRealSense Frame Capture Application

This application sends the specific frame data to LabRecorder during recording. This allows to synchronize recordings during post-processing, by knowing exactly the frames of a particular event and where to process the video. Currently the recordings are stored in H264 (using FFMPEG) and respectively compressed as close to lossless as possible (in order to reduce the amount of storage space). The latter can be changed in the code if required.

# Sensing Tex Pressure Mat Application

This application ties directly into the SensingTex Mat sensor device. It sends each pressure sensor value of the device (256 channels) through Lab Streaming Layer. Similarly to the other applications of this project, to fully take advandage of this code it is necessary to run it in parallel with LabRecorder and link this app to it.

# Empatica E4 Recorder

This application is capable of recording three different streams (BVP, GSR and Temperature) obtained from the Empatica E4 device and push it to LabStreaming Layer. Respective software for the Empatica is necessary such as the Empatica Server, which allows access to the E4 devices. Futhermore, similar to the other applications in this software suite the LabRecorder software is necessary to effectively record the signals.

Side Note: Given that the BVP and GSR/Temp have different sampling rates, each signal type has its own LabStreaming Layer "pipeline", thus it is important to connect the different streams when running LabRecorder.

# AppTest

This is a simple application that was used to do initial tests on LabStreaming Layer. It sends custom messages (markers) to LabRecorder whenever the user presses the send button.

# Random Signal Sender

This is a simple application that was used to do initial tests on LabStreaming Layer. It sends a random value at a frequency of 100Hz to Labrecorder.