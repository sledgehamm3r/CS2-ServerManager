   M S B A M L   `   `   `  ����   CS2ServerManager!    CS2ServerManager.MainWindow   W SPresentationCore, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35e aSystem.Windows.Controls.Ribbon, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089R NWindowsBase, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35F 9http://schemas.microsoft.com/winfx/2006/xaml/presentation     R NSystem.Xaml, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     -     � Title$  CS2 Server Manager��$	��700��$
��1200��$
��1000��$	��600�� � WindowStartupLocation$ CenterScreen=�$��Segoe UI)����#121212 � 	Resources 3  -System.Windows.Media.Effects.DropShadowEffect   DropShadowEffect   Color   
BlurRadius   ShadowDepth   Opacity �� 
TargetType �� Property	 �� Value
 �� Triggers P� Property P� Value �� 
TargetType �� CornerRadius J� SnapsToDevicePixels �� Triggers  ActionButtonStyle �� BasedOn   BooleanToVisibilityConverter��%�  &	        'o� ,     'm� �     '�� �    &	 �    7��'�� �    &	 5      $ Black��$ 15Q�$ 2Q�$	 0.6Q��� " o�.���  � ��	 �#1E1E1E��  � ��
	 �������  � ��$	 16(���  � ��$	 60���� " m�.���  � ��
	 �������  � ��
	 ���� ��  � ��$
	 12,8q���  � ��$		 5,0q���  �@�Cursor$
	 Hand��
 P�  �@J�IsMouseOver$
 True��. ���  � ��	 �#0078D7��  � ��
	 �����P�  �@m�IsSubmenuOpen$
 True��. ���  � ��	 �#0078D7��  � ��
	 ������� " ��.���  � ��	 �#0078D7��  � ��
	 �������  � ��$	 14(���  � ��$
	 10,6q���  � ��$	 5q���  � ��$	 0q���  �@�Cursor$
	 Hand����  � ��$	 38����  � ��$		 130����  � ��	 �� " ��.R��� #��z��$ 6��$
 True��._��� $��Center=�$��Center=� P�  �@J�IsMouseOver$
 True��. ���  � ��	 �#3393DFP�  � ��$
 True��. ���  � ��	 �#006CC1�� " ��8   .���  � ��$	 36����  � ��$	 36����  � ��
	 �������  � ��	 �#2D2D30��  � ��$	 16(���  � ��$	 3q���  � ��	 �� " ��.R��� #��z��$ 18��$
 True��._��� $��Center=�$��Center=� P�  �@J�IsMouseOver$
 True��. ���  � ��	 �#0078D7�� " ��.���  �@��TextDecorations	 Z� ��  � ��	 �#0078D7�� .���  � RowDefinitions �� $��60 ��� $��* ��� $
��Auto �.+��� $��0�����#1E1E1E J� Effect# [  ._�� .+�o� $��Center=�.@�m� -   $��About��m� -   $��Credits��m� -   $��Github���� $��CS2 Server Manager��$��18(�
�������$��Center=�$��Center=�$��SemiBold"��� $��1�����#1A1A1A$ 12�� �� Padding$ 25q�$��15q�# [  ._��  �� $
��Auto ��� $��* ��� $
��Auto �.+�� $��0��$��0,0,0,25q� � ColumnDefinitions �� $
��Auto ��� $��* ��� $
��Auto �.+���  �� Orientation$ 
Horizontal=�$��0��.+��� -    ��Name$ ToggleConsoleButton��$��Toggle Console�� ��%9   '��       �� " ��.���  �@��CornerRadius$	 8���� $��1��$��Server Management��$��28(�
�������$��Center=�$��Center=�$��Light"�$
��0,10q��� -   $ CreateServerButton��$��Create Server��$��2�����#28A745 ��%9   '��       �� " ��.���  �@��CornerRadius$	 8���� $��1��$ 8�����#252526$ 1q�._�&   System.Windows.Controls.DataGrid  -     Name$ ServerDataGrid��$��0q�   AutoGenerateColumns$ False��   
IsReadOnly$
 True��   SelectionMode$ Single=����#252526
�������   RowBackground �#2D2D30   AlternatingRowBackground �#333337   HorizontalGridLinesBrush �#3E3E42   VerticalGridLinesBrush �#3E3E42$��0q�$��15(� =  7System.Windows.Controls.Primitives.DataGridColumnHeader)  #System.Windows.Controls.DataGridRow��%�  '        '  �     �� "  .���  � ��	 �#3C3C3C��  � ��
	 �������  � ��$	 SemiBold"���  � ��$	 10,12q���  � ��$	 0,0,1,0q���  � ��	 �#555555�� "  .���  � ��
	 �������  � ��$	 14(���  � ��$	 45����  � ��$	 0q���  � ��$	 8q�
 P�  �@J�IsMouseOver$
 True��. ���  � ��	 �#404045P�  �@ 
IsSelected$
 True��. ���  � ��	 �#0078D7    Columns  0  *System.Windows.Controls.DataGridTextColumn  ,  &System.Windows.Controls.DataGridColumn!   Header$! Server Name��"   Width5  /System.Windows.Controls.DataGridLengthConverter$" * 1  +System.Windows.Controls.DataGridBoundColumn#   Binding# �� *Name+  $
! Port��$
" 0.5* # �� *Port+  $! Status��$
" 0.7* # �� *Status+4	  .System.Windows.Controls.DataGridTemplateColumn	  $! Actions��$
" Auto $ 	  CellTemplate$ �� .R��� $ 
Horizontal=�$��Center=�.+��� -   $��✏️��% � ToolTip$
% Edit��#��[ �� -	   $��🗑️��$% Delete��#��[ ���#E04747�� -
   $��▶️��$% Start��#��[ ���#28A745�� -   $��⏹️��$
% Stop��#��[ ���#DC3545�� -   $��⬇️��$% Download Server Files��#��[ ���#17A2B8�� $��2�����#252526$ 8��$ 12q�$��0,20,0,0q�._�� .+��� $ 
Horizontal=�$
��Left=�$��Center=�.+��� $ 
Horizontal=�$��Center=�y��� *IsProgressVisible+& �� 	Converter#& [ .+�� -   ' �Name$' FooterProgressBar��$	��220��$��22��( � Minimum$( 0Q�) � Maximum$	) 100Q�$��0,0,10,0q��� -   * ��Name$* FooterStatusText��
�������$��Center=�$��14(�$��0,0,15,0q��� $ 
Horizontal=�$��Right=�$��Center=�.+��� $��Version 1.0.0�����#AAAAAA$��Center=�$	��5,0q��� $	�� | �����#AAAAAA$��Center=�$	��5,0q��� $��Center=�$	��5,0q��� -   $%��https://github.com/sledgehamm3r?�.�.��� $!��/Resources/Icons/github.png��$��20��$��20���� -   + ��Name$+ FooterPanel��$��2�����#252526$��15q�$ 8��$ 12q�$	��170��$��Bottom=�$y�	Collapsed=�._��� -   , ��Name$, FooterConsoleTextBox�����#1A1A1A���#00FF7F$��Cascadia Code, Consolas)�$��12(�- �� 
IsReadOnly$
- True��. �� TextWrapping$
. Wrap=�"/ �� VerticalScrollBarVisibility$
/ Auto=�$��0q�