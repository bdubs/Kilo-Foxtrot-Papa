   M S B A M L   `   `   `  ���� ^  ZMicrosoft.Samples.Kinect.WpfViewers, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null�^clr-namespace:Microsoft.Samples.Kinect.WpfViewers;assembly=Microsoft.Samples.Kinect.WpfViewers#Microsoft.Samples.Kinect.WpfViewers  # MicrosoftKinectVoiceGameFunTime    KinectAudioDemo.MainWindow   W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     mkv^clr-namespace:Microsoft.Samples.Kinect.WpfViewers;assembly=Microsoft.Samples.Kinect.WpfViewers     -     � Title$3  -Kinect Audio Demo - Sound Source Localization��$	��639��$	��824�� � WindowState$ 	Maximized=�.���� -   $��canvas1��$	��800��$	��600��$	��Top=�
���   �.+��� -   $��image1�� �� Left$ 0��
 �� Top$ 0��$	��600��$
��Fill=�$��airfield3.jpg��$	��800���� -   $��ColorCameraImage��$	��150��$
��Fill=�$	��200��$	 600��$ 0���� $
��Left=�$��Bottom=�.+��� -    ��Name$ bdConf��$	��700��$��30��
������ $��3q�=   7Microsoft.Samples.Kinect.WpfViewers.KinectSensorChooser  -     Name$ SensorChooser��$��Center=�$��Center=�$��350,224,0,166q�$	 -90��$ 19���� -   $��currentCallSign��$�� ��$��42��$
��Left=�$��
13,197,0,0q�$��24(�$	��Top=�$	��234��
�������$	 406��$	 -69���� -   $��lastUsed��$�� ��$��43��$
��Left=�$��
12,140,0,0q�$��24(�$	��Top=�$	��169��
�������$	 406��$ 26������ �� -	   $��lastCallSign��$��Last Callsign:��$��54��$��24(�$
��Left=�$��
13,120,0,0q�$	��Top=�$	��145��
�������$	 252��$ 45���� -
   $��planeLabel1��$��planeLabel1��
�������$��43��$
��Left=�$��
608,12,0,0q�$	��Top=�$
 -596��$ 76��$��24(�$	��138���� -   $��planeLabel2��$��planeLabel2��$��24(�
�������$��45��$
��Left=�$��
608,46,0,0q�$	��Top=�$
 -596��$	 100��$	��138���� -   $��planeLabel3��$��planeLabel3��$��24(�
�������$��50��$
��Left=�$��
608,80,0,0q�$	��Top=�$
 -595��$	 114��$	��138���� -   $��BackdropImage��$	��150��$
��Left=�$��260,114,0,0q�$
��Fill=�$	��Top=�$	��200��$
 -213��$ 83��$y�Hidden=�A� -   $��rightLander��$��0,0,0,0q����	#FFFFC10A$
��Left=�$	��162��$��60��$	��Top=�$ 85��$	 301����Z� �� -    ��Name$ tbSpeechStatus��$�� ��
���   �$��60��$��16(� �� TextAlignment$
 Left=� �� Padding$ 0,0,0,0q�$	 614��$	 146���� -   $��runwayRect3��$	 561��$	 455��$	��145��$	��207��$y�Hidden=��� -   $��runwayRect2��$	 292��$	 455��$	��145��$	��207��$y�Hidden=��� -   $��runwayRect1��$ 28��$	 455��$	��145��$	��207��$y�Hidden=��� -   $��capturedPlaneNumber��$	 108��$	 301��$�� ��$��	ExtraBold"�$��46��
�������$��18(�$	��107��$��Center=�$��Center=�	 � DataContext	 �� A� -   $��doNotEnter3��$	 630��$	 511��
���  ��$��60��
���   �$��60��A� -   $��doNotEnter2��$	 367��$	 511��
���  ��$��60��
���   �$��60��A� -   $��doNotEnter1��$ 96��$	 511��
���  ��$��60��
���   �$��60���� -   $��label1��$	 228��$	 128��$��Current Callsign:��$��24(�
�������$��54��$	��185��� -   $��	fuelTank1��$ 16��$	 131��$��19��$	��140��
 � Value$	
 100Q� � Maximum$
 1800Q�� -   $��	fuelTank2��$ 16��$	 184��$��19��$	��140��$	
 100Q�$
 1800Q�� -   $��	fuelTank3��$ 16��$	 234��$��19��$	��140��$	
 100Q�$
 1800Q��� -   $��blackBackgroundRectangle��$	��800��$	��600��
���   �$y�Hidden=��� -   $��theBigSecret��$	��500��$	��300��$��poptart1red1.gif��$	 153��$	 165��$y�Hidden=��� -   $��scoreNum��$	 659��$	 166��$��0��$��37��$��18(�$	��177���� -   $��
scoreLabel��$	 600��$	 165��$��Score:��$��18(�$��37��$��61���� -   $��label2��$	 306��$	 105��$��
Gamer Over��$��66��$	��201��
���  ��$��36(�$��	Game Over)�$y�Hidden=��� -    $��pausedLabel��$	 344��$	 106��$��Paused��$��	Game Over)�$��36(�
���  ��$��66��$y�Hidden=�$	��201��