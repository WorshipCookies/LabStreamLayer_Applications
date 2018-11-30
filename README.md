# ApplicationLSL_Test

This is an application suite that I am currently working on for multimodal data collection experiments. Applications developed in this suite all work on top of the LabStreaming Layer middleware allowing to fully synchronize different devices. This code is currently a work in progress and will be slowly updated as time goes on.

# Making it Run

All these applications are running in x64, so when compiling make sure that you are compiling in x64 (not CPU or x86). All code was developed in Visual Studio 2017, so it should work out of the box in that system. I haven't tested on other compilers. 

It is also important to obtain the LabRecorder from the original LabStreaming Layer software, as all devices will be linked to this software. Once all devices are linked LabRecorder will take care of synchronizing all of the different applications.

# Dependencies

The majority of this software was developed on the .NET Framework 4.7.1., futhermore given that these applications are loading external dlls (i.e. LabStreaming Layer and Bitalino) it is necessary to also download and install the Microsoft Visual VC++ Redistributable 2013 and 2017 (both x86 and x64).

# BitalinoRecorder Application

This application is capable of recording several channels from the Bitalino Device. Currently, it is set to record 3 channels, which was tested on ECG (Electrocardiogram), EDA (Electrodermal Activity) and Respiration. To fully make it work it is necessary to run LabRecorder in parallel and link this app to it.

This Application supports "parametrized execution", which kickstarts the recording process immediatly if run with a PlayerID Parameter and a BITalino Device Index Number. Example: C:\BitalinoRecorder.exe X,Y; where X is the player ID and the Y is the BITalino Device Index in the Device List of the Interface. Important: Bitalino must be connected through Bluetooth for this method to work.

# IntelRealSense Frame Capture Application

This application sends the specific frame data to LabRecorder during recording. This allows to synchronize recordings during post-processing, by knowing exactly the frames of a particular event and where to process the video. Currently the recordings are stored in H264 (using FFMPEG) and respectively compressed as close to lossless as possible (in order to reduce the amount of storage space). The latter can be changed in the code if required.

This Application supports "parametrized execution", which kickstarts the recording process immediatly if run with a PlayerID Parameter. Example: C:\IntelRealSense-FrameCapture.exe X; where X is the player ID.

# Sensing Tex Pressure Mat Application

This application ties directly into the SensingTex Mat sensor device. It sends each pressure sensor value of the device (256 channels) through Lab Streaming Layer. Similarly to the other applications of this project, to fully take advandage of this code it is necessary to run it in parallel with LabRecorder and link this app to it.

This Application supports "parametrized execution", which kickstarts the recording process immediatly if run with a PlayerID and COM Port Number Parameter. Example: C:\SensingTex-PressureMat.exe X Y; where X is the player ID and Y is the COM Port Number associated to the Pressure Sensing Mat.

# Empatica E4 Recorder

This application is capable of recording three different streams (BVP, GSR and Temperature) obtained from the Empatica E4 device and push it to LabStreaming Layer. Respective software for the Empatica is necessary such as the Empatica Server, which allows access to the E4 devices. Futhermore, similar to the other applications in this software suite the LabRecorder software is necessary to effectively record the signals.

Side Note: Given that the BVP and GSR/Temp have different sampling rates, each signal type has its own LabStreaming Layer "pipeline", thus it is important to connect the different streams when running LabRecorder.

This Application supports "parametrized execution", which kickstarts the recording process immediatly if run with a PlayerID Parameter. Example: C:\EmpaticaE4-Recorder.exe X; where X is the player ID. Important: Empatica Server must be running and have an associated device to work.

# Script Execution Application

This is a very simple program that allows to batch run executables from an ExecutionCommands.txt. This was created simply for experiment support. It allows to batch run the LSL Recorders required during experiment deployment, facilitating the protocol and reducing the number of errors during the actual experimental process.

In order to use the ExecutionCommands.txt expects a path in every newline as such:
PATH\TO\EXECUTABLE.EXE,EXE_PARAM_1 EXE_PARAM_2 ...

# AppTest

This is a simple application that was used to do initial tests on LabStreaming Layer. It sends custom messages (markers) to LabRecorder whenever the user presses the send button.

# Random Signal Sender

This is a simple application that was used to do initial tests on LabStreaming Layer. It sends a random value at a frequency of 100Hz to Labrecorder.
