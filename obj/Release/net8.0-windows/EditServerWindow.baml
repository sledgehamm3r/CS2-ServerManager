   M S B A M L   `   `   `  ����   CS2ServerManager'    !CS2ServerManager.EditServerWindow   W SPresentationCore, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35e aSystem.Windows.Controls.Ribbon, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089R NWindowsBase, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35F 9http://schemas.microsoft.com/winfx/2006/xaml/presentation     R NSystem.Xaml, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml       � Title$  Edit Server��$	��650��$	��700�� � WindowStartupLocation$ CenterScreen=����#121212 � 	Resources 3  -System.Windows.Media.Effects.DropShadowEffect   DropShadowEffect   Color   
BlurRadius   ShadowDepth   Opacity �� 
TargetType �� Property	 �� Value
 �� 
TargetType �� CornerRadius J� SnapsToDevicePixels �� Triggers P� Property P� Value ��Name ��Name �� 
TargetName ��Name d�Name   ComboBoxToggleButtonTemplate �� 	ClickMode d� 	IsChecked �� Path �� Mode �� RelativeSource J� IsHitTestVisible �� SelectionBoxItem �� SelectionBoxItemTemplate ��Name  ComboBoxTextBoxTemplate �� 
IsReadOnly �� 
IsReadOnly  �Name! �� IsDropDownOpen" � AllowsTransparency# �Name$ � ActualWidth% �� MaxDropDownHeight& K� IsItemsHost-  'System.Windows.Input.KeyboardNavigation'   DirectionalNavigation( � ColumnDefinitions) F�Name��%?
  &	        '�� ,     '�� �    '�� �    '�� �    '�� �    &	 �    '�� �    '�� �    '�� 	    '�� �	      $ Black��$ 15Q�$ 2Q�$	 0.6Q��� " ��.���  � ��	 �#0078D7��  � ��
	 �������  � ��$	 14(���  � ��$
	 10,6q���  � ��$	 5q���  � ��$	 0q���  �@�Cursor$
	 Hand����  � ��	 �� "
 ��.R��� #��z��$ 6��$
 True��._��� $��Center=�$��Center=� P�  �@J�IsMouseOver$
 True��. ���  � ��	 �#3393DFP�  � ��$
 True��. ���  � ��	 �#006CC1�� " ��.���  � ��
	 ���� ��  � ��$	 0q��� " ��.���  � ��	 �� "
 ��.R��� $ Border�����#252526$ 6,6,0,0��$��0,0,4,0q�._��� $ ContentSite��$��Header��$��Center=�$��Center=�$
��12,8q�$
��True�� P�  �@��
IsSelected$
 True��. ��� $ Border�� � ��	 �#0078D7P�  �@J�IsMouseOver$
 True��. ��� $ Border�� � ��	 �#3E3E42��  � ��
	 �������  � ��$	 14(���  � ��$	 0,0,0,0q��� " ��.���  � ��	 �#2D2D30��  � ��
	 �������  � ��$	 1q���  � ��	 �#3E3E42��  � ��$		 8,6q���  � ��	 �� "
 ��.R��� #��z��#��z��#��z��$ 4��._��� $ PART_ContentHost���� " ��.���  � ��	 �#2D2D30��  � ��
	 �������  � ��$	 1q���  � ��	 �#3E3E42��  � ��$		 8,6q���  � ��	 �� "
 ��.R�� .+�d� $ ToggleButton��#���  $��2��$|�false��$ Press=� �� $ IsDropDownOpen�$ TwoWay=� �� *TemplatedParent+�� $ ContentSite��$ False��#��z #��z #��z��$��8,3,32,3q�$��Center=�$
��Left=��� $ PART_EditableTextBox��#���  $
��Left=�$��Center=�$��3,3,32,3q�$
|�True��
������ $y�Hidden=�# z ��Z� � $  Popup��$��Bottom=�#��z! $
" True��$|�False��$��Slide=�.��� $# DropDown��$
 True��#��z$ #��z% .+��� $ DropDownBorder�����#2D2D30$��1q����#3E3E42$ 0,0,4,4���� $	��4,6q�.���� $
& True��$' 	Contained=��� "
 d�.R�� ( �� �� $��20 �.+��� $ Border��$��2��$ 4�����#2D2D30���#3E3E42$��1q��� $��0��$ 4,0,0,4��$��1q�F� $) Arrow��$��1��
�������$��Center=�$��Center=�"����        � 	=  	= � z     	�� " ��.���  � ��
	 �������  � ��$	 5q��� " ��.���  � ��
	 �������  � ��$	 Center=��� " ��.���  � ��	 �#252526��  � ��
	 �������  � ��$	 1q���  � ��	 �#3E3E42�� " ��.���  �@��TextDecorations	 Z� ��  � ��	 �#0078D7.���� ���#1A1A1A$��10q�$ 10��* J� Effect#* [  ._�� $��15q�.+��� .@��� $��Server Settings��.���� $
��Auto=�.��� $��15,20,15,15q�+ � RowDefinitions+ �� $
��Auto ��� $
��Auto ��� $
��Auto ��� $
��Auto ��� $
��Auto ��� $
��Auto ��� $
��Auto ��� $
��Auto ��� $
��Auto ��� $
��Auto ��� $
��Auto �( �� $
��Auto ��� $��* �.+��� $��Port:��$��0��$��0��$��Center=�$
��5,10q�$��SemiBold"��� -   $ PortTextBox��$��0��$��1��$
��5,10q��� $��1��$��0��$
��5,10q�$��Center=�$��SemiBold"�Map: �� -   $��https://example.com/mapid?�.�Info�� -   , ��Name$, MapComboBox��$��1��$��1��$
��5,10q�.@��� $��
de_ancient��
- � Tag$- standard���� $��	de_anubis��$- standard���� $��de_dust2��$- standard���� $��
de_inferno��$- standard���� $��cs_italy��$- standard���� $��	de_mirage��$- standard���� $��de_nuke��$- standard���� $��	cs_office��$- standard���� $��de_overpass��$- standard���� $��
de_vertigo��$- standard���� $��custom���� -   $ CustomMapTextBox��$��2��$��1��$
��5,10q�$��Map ID��$y�	Collapsed=��� -   $��Download Workshop Map��$��2��$��0��$
��5,10q�y��� . �� ElementName$. CustomMapTextBox��$ 
Visibility��� $��3��$��0��$
��5,10q�$��Center=�$��SemiBold"�Game Mode: �� -   $"��https://example.com/gamemode?�.�Info�� -   $ GameModeTextBox��$��3��$��1��$
��5,10q��� $��4��$��0��$
��5,10q�$��Center=�$��SemiBold"�Gameserver Token: �� -   $+��%https://example.com/steamaccounttoken?�.�Info�� -	   $ SteamAccountTokenTextBox��$��4��$��1��$
��5,10q��� $��Max Players:��$��5��$��0��$��Center=�$
��5,10q�$��SemiBold"��� -
   $ MaxPlayersTextBox��$��5��$��1��$
��5,10q��� $��RCON Password:��$��6��$��0��$��Center=�$
��5,10q�$��SemiBold"��� -   $ RconPasswordTextBox��$��6��$��1��$
��5,10q��� $��Server Tags:��$��7��$��0��$��Center=�$
��5,10q�$��SemiBold"��� -   $ ServerTagsTextBox��$��7��$��1��$
��5,10q��� $��Insecure (Disable VAC?):��$��8��$��0��$��Center=�$
��5,10q�$��SemiBold"��� -   / ��Name$/ InsecureCheckBox��$��8��$��1��$��Center=�$��10q��� $��Auto-Restart:��$��9��$��0��$��Center=�$
��5,10q�$��SemiBold"��� -   $/ AutoRestartCheckBox��$��9��$��1��$��Center=�$��10q��� $��10��$��2��0 �� Orientation$0 
Horizontal=�$��Right=�$��5,20,5,5q�.+��� -   $
��Save��$	��100��$��36��$��5q����#28A745
�������$��SemiBold"��� -   $��Cancel��$	��100��$��36��$��5q����#DC3545
�������$��SemiBold"��� $��RCON Console��.��� $��15,20,15,15q�+ �� $��* ��� $
��Auto �.+��� $��0�����#252526���#3E3E42$��1q�$ 5��$��0,0,0,10q�._��� -   $ RconOutputTextBox��$
 True��1 �� AcceptsReturn$
1 True��"2 �� VerticalScrollBarVisibility$
2 Auto=�$��0q�$��0q�
������ ���#00FF7F$��Cascadia Code, Consolas)�� $��1��$��0q�( �� $��* ��� $
��Auto �.+��� -   $ RconCommandTextBox��$��0��$��0,0,10,0q��� -   3 ��Name$3 SendRconButton��$��1��$
��Send��$	��100��$��36���� $��Plugins��.��� $��15,20,15,15q�.+��� ���#252526$ 8��4 �� Padding$4 20q�._��� $��Center=�$��Center=�.+��� $��Plugins / Mods��$��22(�$��SemiBold"�$��Center=�$��
10,5,10,20q��� -   $%3 InstallCounterStrikeSharpButton��$ ��Install CounterStrikeSharp��$	��250��$��45��$
��10,5q�$��Center=��� -   5 ��Name$5 PluginManagerPanel��$y�	Collapsed=�$��10,20q�.+��� ���#1E1E1E$ 6��$��1q����#3E3E42._��� -   6 ��Name$6 PluginListView��$��0q�$	��250��
������ $��0q����� .R��� ���#252526$4 8q�$��2q�$ 4��$	��480��._�� ( �� $��* ��� $
��Auto ��� $
��Auto �.+��� $��Center=�$��0��$��CharacterEllipsis=����� �� -   $
��Open��$��1��$��80��$	��5,0q����#0078D7- �� �� -   $��Delete��$��2��$��80��$	��5,0q����#DC3545- �� �� -   $3 UploadPluginButton��$��Upload Plugin (.zip)��$	��200��$��40��$��10,20,10,10q�$��Center=��� -   $��Config��.��� $��15,20,15,15q�+ �� $
��Auto ��� $��* ��� $
��Auto ��� $
��Auto ��� $
��Auto ��� $
��Auto ��� $
��Auto �.+��� -   7 ��Name$7 AutoSaveIndicatorText��$��0��$��Bereit��$��Right=�$��0,0,5,10q����#AAAAAA$��12(��� $��1�����#252526���#3E3E42$��1q�$ 5��$��0,0,0,15q�._��� -   $ ConfigTextBox��$
1 True��8 �� 
AcceptsTab$
8 True��$
2 Auto=�$9 �� HorizontalScrollBarVisibility$
9 Auto=�$��0q�$��0q�
������ $��Cascadia Code, Consolas)�: �� TextWrapping$
: Wrap=��� $��2��$��4�����#252526$ 8��$4 15q�$��0,0,0,15q�._��� .+�� $	��0,5q�( �� $��* ��� $
��Auto ��� $
��Auto �.+��� $��Prac Config:��$��Center=�$��SemiBold"��� -   $/ PracConfigCheckBox��$��1��$
��10,0q��� -   $
��Edit��$��2��$	��5,0q�$��85���� $
��0,10q����#3E3E42� $	��0,5q�( �� $��* ��� $
��Auto ��� $
��Auto �.+��� $��5v5 Config:��$��Center=�$��SemiBold"��� -   $/ FiveVFiveConfigCheckBox��$��1��$
��10,0q��� -    $
��Edit��$��2��$	��5,0q�$��85���� $
��0,10q����#3E3E42� $	��0,5q�( �� $��* ��� $
��Auto ��� $
��Auto �.+��� $��2v2 Config:��$��Center=�$��SemiBold"��� -!   $/ TwoVTwoConfigCheckBox��$��1��$
��10,0q��� -"   $
��Edit��$��2��$	��5,0q�$��85���� $
��0,10q����#3E3E42� $	��0,5q�( �� $��* ��� $
��Auto ��� $
��Auto �.+��� $��Deathmatch Config:��$��Center=�$��SemiBold"��� -#   $/ DeathmatchConfigCheckBox��$��1��$
��10,0q��� -$   $
��Edit��$��2��$	��5,0q�$��85���� $��6��$0 
Horizontal=�$��Right=�$��0q�.+��� -%   $
��Load��$��90��$��36��$��5q��� -&   $
��Save��$��90��$��36��$��5q����#28A745�� -'   $��Backup��$��90��$��36��$��5q����#17A2B8