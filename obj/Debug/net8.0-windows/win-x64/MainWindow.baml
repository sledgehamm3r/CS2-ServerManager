   M S B A M L   `   `   `  ����  CS2ServerManager!    CS2ServerManager.MainWindow   5   	   W SPresentationCore, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35e aSystem.Windows.Controls.Ribbon, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089R NWindowsBase, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35F 9http://schemas.microsoft.com/winfx/2006/xaml/presentation     5   	   R NSystem.Xaml, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   -   5          � Title$  CS2 Server Manager��5   	   $	��700��6$   $
��1200��61    � WindowStartupLocation$ CenterScreen=�5   	   ���#2D2D30$��Segoe UI)�5   	    � 	Resources 5
       �� 
TargetType �� Property �� Value �� 
TargetType �� CornerRadius    BooleanToVisibilityConverter��%�  '��       '�� .    &	  �    �� 5   
   " ��6   .�5      ��  � �� �#007ACC6+   �� 5       � ��
 �����6+   �� 5       � ��$ 14(�6)   �� 5       � ��$	 5,2q�6(   �� 5       � ��$ 2q�6'   �� 5       � ��$ 0q�60   �� 5       �@�Cursor$
 Hand��6'   �� 5       � ��$ 30��6'   �� 5       � ��$	 100��6&   �� 5       � �� 5      �� 5      " ��6&   .R�5      �� #��z��6!   $ 5��6K   ._�5      �� $��Center=�6/   $��Center=�6L   5      5      5      5      �� 5   
   " ��6   .�5       ��  �@��TextDecorations 60   Z� 5!      �� 5"   
   6'   5#      .��5$      � $��10q�6    � RowDefinitions 5%   
   �� 5&      $
��Auto �6   �� 5'      $��* �6   �� 5(      $
��Auto �6   �� 5)      $
��Auto �6   	 � ColumnDefinitions	 5+   
   �� 5,      $��* �6   �� 5-      $
��Auto �6   .+�51   
   �� -   5        
 ��Name$
 ToggleConsoleButton��51      $��Toggle Console��6.   $
��Left=�52      $	��Top=�6,   $��10q�6D   �� 55   
   -   5        $
 CreateServerButton��55      $��Create Server��6-   $��Right=�56      $	��Top=�6-   $��10q�6E   $��1��6Q   �� 58   
   $��CS2 Server Manager��6   $��32(�6.   
�������$��Center=�59   '   $��	0,10,0,20q�5:      $��0��6'   $��2��64   &   System.Windows.Controls.DataGrid  5;   
   -   5          Name$ ServerDataGrid��5;      $��1��6+   $��2��68   $��0,0,0,10q�6L      AutoGenerateColumns$ False��5<         
IsReadOnly$
 True��6/      SelectionMode$ Single=�6A   ���#1E1E1E
�������   RowBackground �#252526   AlternatingRowBackground �#2D2D30   HorizontalGridLinesBrush
 ����    VerticalGridLinesBrush
 ���� $��16(�5@       5A      =  7System.Windows.Controls.Primitives.DataGridColumnHeader)  #System.Windows.Controls.DataGridRow��%�  '        '  �     �� 5B      "  6   .�5C      ��  � �� �#3C3C3C63   �� 5D       � ��
 �����63   �� 5E       � ��$
 Bold"�63   5F      �� 5G      "  6   .�5H      ��  � ��
 �����63   �� 5I       � ��$ 16(�61   �� 5J       � ��$ 35��6/   �� 5K       � ��$ 2q�6/   �� 5L       � ��$ 5q�60   5M      5N         Columns 5O      0  *System.Windows.Controls.DataGridTextColumn  5P      ,  &System.Windows.Controls.DataGridColumn   Header$ Server Name��6%      Width5  /System.Windows.Controls.DataGridLengthConverter$ * 6S   1  +System.Windows.Controls.DataGridBoundColumn   Binding 6:   �� *Name+6S     5Q      $
 Port��6%   $ * 6L    63   �� *Port+6L     5R      $ Status��6%   $ * 6P    65   �� *Status+6P   4  .System.Windows.Controls.DataGridTemplateColumn  5S      $ Aktionen��6)      CellTemplate 5T      �� 5U      .R�5V      ��  �� Orientation$ 
Horizontal=�6)   .+�5W   "   �� -   5        $��✏️��5W   )    � ToolTip$ 
Bearbeiten��66   $��2q�6d   $��30��6o   $��30��6z   �� 5X   "   -   5        $��🗑️��5X   )   $ Löschen��67   $��2q�6d   $��30��6o   $��30��6z   �� 5Y   "   -   5        $��▶️��5Y   )   $ Starten��66   $��2q�6b   $��30��6m   $��30��6x   �� 5Z   "   -   5        $��⏹️��5Z   )   $ Stoppen��66   $��2q�6a   $��30��6l   $��30��6w   �� 5[   "   -	   5        $��⬇️��5[   )   $ Download Server Files��66   $��2q�6x   $��30��6�   $��30��6�   5\      5]      5_      5a      �� 5b   
   $��3��6   $��2��6"   $ 
Horizontal=�66   $��Center=�6O   $��Center=�6l   $��10q�6�   .+�5c      �� $ 
Horizontal=�6   $
��Left=�62   $��Center=�6M   y�6h   �� *IsProgressVisible+ �� 	Converter# [  .+�5d      � -
   5         �Name$ FooterProgressBar��5d      $	��220��69   $��22��6E    � Minimum$ 0Q�5e       � Maximum$	 100Q�6*   $��0,0,10,0q�68   �� 5f      -   5         ��Name$ FooterStatusText��5f      
�������$��Center=�6I   $��14(�5g      $��0,0,10,0q�6*   5h      �� 5i      $��Version 1.0.0��6   
�������$��5q�6@   �� 5j      $	�� | ��6   
�������$��5q�66   �� 5k      $��Center=�6   �� 5l      -   5        $%��https://github.com/sledgehamm3r?�5l      .�6v   .�5m      �� $!��/Resources/Icons/github.png��6   $��16��6A   $��16��6L   5n      5o      5p      �� 5q   
   -   5         ��Name$ FooterPanel��5r      $��2��5q      $��2��6"   $ Vertical=�66   ���#3F3F46$��10q�6b   $y�	Collapsed=�5r   *   .+�5s      �� -   5          ��Name$  FooterConsoleTextBox��5s      $��0,10,0,0q�64   
���   �
������$��Consolas)�5t   A   $��12(�5u      $	��150��6$   ! �� 
IsReadOnly$
! True��61   " �� TextWrapping$
" Wrap=�6C   "# �� VerticalScrollBarVisibility$
# Auto=�6W   5v      5w      5x      