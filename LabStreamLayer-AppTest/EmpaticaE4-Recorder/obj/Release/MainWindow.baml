   M S B A M L   `   `   `  ���� N  JEmpaticaE4-Recorder, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null9!clr-namespace:EmpaticaE4_RecorderEmpaticaE4_Recorder  $    EmpaticaE4_Recorder.MainWindow   \ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     @d2http://schemas.microsoft.com/expression/blend/2008     Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     3local!clr-namespace:EmpaticaE4_Recorder       � Title$  Empatica E4 Recorder��$	��450��$	��800��.��� -    �Name$ IDDeviceLabel��.+��� -    ��Name$ ConnectDevice��$��Connect Device��$
��Left=�$��70��$��	24,58,0,0q�$	��Top=�$	��161���� -    ��Name$ DataReceivedTextBlock��$
��Left=�$	��199��$��
24,192,0,0q�$
��Wrap=�$	��Top=�$	��743��{�.  �� -    ��Name$ IDDeviceList��$
��Left=�$	��119��$��
647,48,0,0q�$	��Top=�$	��120���� $��Available Devices:��$��23��$��647,20,25,0q�$	��Top=��� -   $ refreshDeviceListButton��$��Refresh List��$
��Left=�$��
565,81,0,0q�$	��Top=�$��77��$��46���� -    ��Name$ BVPStreamBox��$	��BVP��$
��Left=�$��
213,68,0,0q�$	��Top=�$��46�� d� 	IsChecked$
 TrueY��� -   $ GSRStreamBox��$	��GSR��$
��Left=�$��
213,88,0,0q�$	��Top=�$
 TrueY��� -   $ TempStreamBox��$��Skin Temperature��$
��Left=�$��213,108,0,0q�$	��Top=�$
 TrueY��� -	    ��Name$ StreamLabel��$��Streams:��$
��Left=�$��23��$��
204,40,0,0q�$	��Top=�$��55���� -
   $ playerIDLabel��$��
Player ID:��$
��Left=�$��	24,20,0,0q�$	��Top=�$��29��$��61���� -    ��Name$ playerIDTextBox��$
��Left=�$��23��$��	90,24,0,0q�	 �� TextWrapping$
	 Wrap=�$��P1��$	��Top=�$��23��