   M S B A M L   `   `   `  ����   CS2ServerManager!    CS2ServerManager.MainWindow   W SPresentationCore, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35e aSystem.Windows.Controls.Ribbon, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089R NWindowsBase, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35F 9http://schemas.microsoft.com/winfx/2006/xaml/presentation     R NSystem.Xaml, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     -     � Title$  CS2 Server Manager��$	��700��$
��1200�� � WindowStartupLocation$ CenterScreen=����#2D2D30$��Segoe UI)� � 	Resources  �� 
TargetType �� Property �� Value �� 
TargetType �� CornerRadius    BooleanToVisibilityConverter��%�  '��       '�� 8    &	  k    �� " ��.���  � �� �#007ACC��  � ��
 �������  � ��$ 14(���  � ��$	 5,2q���  � ��$ 2q���  � ��$ 0q���  �@�Cursor$
 Hand����  � ��$ 30����  � ��$	 100����  � �� �� " ��.R��� #��z��$ 5��._��� $��Center=�$��Center=��� " ��.���  �@��TextDecorations Z� �� .��� $��10q� � RowDefinitions �� $
��Auto ��� $��* ��� $
��Auto ��� $
��Auto �	 � ColumnDefinitions	 �� $��* ��� $
��Auto �.+��� -   
 ��Name$
 ToggleConsoleButton��$��Toggle Console��$
��Left=�$	��Top=�$��10q��� -   $
 CreateServerButton��$��Create Server��$��Right=�$	��Top=�$��10q�$��1���� $��CS2 Server Manager��$��32(�
�������$��Center=�$��	0,10,0,20q�$��0��$��2��&   System.Windows.Controls.DataGrid  -     Name$ ServerDataGrid��$��1��$��2��$��0,0,0,10q�   AutoGenerateColumns$ False��   
IsReadOnly$
 True��   SelectionMode$ Single=����#1E1E1E
�������   RowBackground �#252526   AlternatingRowBackground �#2D2D30   HorizontalGridLinesBrush
 ����    VerticalGridLinesBrush
 ���� $��16(� =  7System.Windows.Controls.Primitives.DataGridColumnHeader)  #System.Windows.Controls.DataGridRow��%�   '        '  Y     �� "  .���  � �� �#3C3C3C��  � ��
 �������  � ��$
 Bold"��� "  .���  � ��
 �������  � ��$ 16(���  � ��$ 35����  � ��$ 2q���  � ��$ 5q�   Columns 0  *System.Windows.Controls.DataGridTextColumn  ,  &System.Windows.Controls.DataGridColumn   Header$ Server Name��   Width5  /System.Windows.Controls.DataGridLengthConverter$ * 1  +System.Windows.Controls.DataGridBoundColumn   Binding �� *Name+  $
 Port��$ *  �� *Port+  $ Status��$ *  �� *Status+4  .System.Windows.Controls.DataGridTemplateColumn  $ Aktionen��   CellTemplate �� .R���  �� Orientation$ 
Horizontal=�.+��� -   $��✏️�� � ToolTip$ 
Bearbeiten��$��2q�$��30��$��30���� -   $��🗑️��$ Löschen��$��2q�$��30��$��30���� -   $��▶️��$ Starten��$��2q�$��30��$��30���� -   $��⏹️��$ Stoppen��$��2q�$��30��$��30���� -	   $��⬇️��$ Download Server Files��$��2q�$��30��$��30���� $��3��$��2��$ 
Horizontal=�$��Center=�$��Center=�$��10q�.+��� $ 
Horizontal=�$
��Left=�$��Center=�y��� *IsProgressVisible+ �� 	Converter# [  .+�� -
    �Name$ FooterProgressBar��$	��220��$��22�� � Minimum$ 0Q� � Maximum$	 100Q�$��0,0,10,0q��� -    ��Name$ FooterStatusText��
�������$��Center=�$��14(�$��0,0,10,0q��� $��Version 1.0.0��
�������$��5q��� $	�� | ��
�������$��5q��� $��Center=��� -   $%��https://github.com/sledgehamm3r?�.�.��� $!��/Resources/Icons/github.png��$��16��$��16���� -    ��Name$ FooterPanel��$��2��$��2��$ Vertical=����#3F3F46$��10q�$y�	Collapsed=�.+��� -     ��Name$  FooterConsoleTextBox��$��0,10,0,0q�
���   �
������$��Consolas)�$��12(�$	��150��! �� 
IsReadOnly$
! True��" �� TextWrapping$
" Wrap=�"# �� VerticalScrollBarVisibility$
# Auto=�