<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class Main
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()> _
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Me.components = New System.ComponentModel.Container()
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(Main))
        Me.Timer1 = New System.Windows.Forms.Timer(Me.components)
        Me.MonitorOutput = New System.Windows.Forms.ComboBox()
        Me.Button2 = New System.Windows.Forms.Button()
        Me.PictureBox1 = New System.Windows.Forms.PictureBox()
        Me.connectionStatus = New System.Windows.Forms.Label()
        Me.flConnectionTimer = New System.Windows.Forms.Timer(Me.components)
        CType(Me.PictureBox1, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'Timer1
        '
        Me.Timer1.Enabled = True
        Me.Timer1.Interval = 1000
        '
        'MonitorOutput
        '
        Me.MonitorOutput.Font = New System.Drawing.Font("Microsoft Sans Serif", 10.8!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.MonitorOutput.FormattingEnabled = True
        Me.MonitorOutput.Location = New System.Drawing.Point(52, 24)
        Me.MonitorOutput.Name = "MonitorOutput"
        Me.MonitorOutput.Size = New System.Drawing.Size(242, 30)
        Me.MonitorOutput.TabIndex = 0
        '
        'Button2
        '
        Me.Button2.Location = New System.Drawing.Point(300, 24)
        Me.Button2.Name = "Button2"
        Me.Button2.Size = New System.Drawing.Size(81, 32)
        Me.Button2.TabIndex = 3
        Me.Button2.Text = "Settings"
        Me.Button2.UseVisualStyleBackColor = True
        '
        'PictureBox1
        '
        Me.PictureBox1.Image = CType(resources.GetObject("PictureBox1.Image"), System.Drawing.Image)
        Me.PictureBox1.Location = New System.Drawing.Point(1, 22)
        Me.PictureBox1.Name = "PictureBox1"
        Me.PictureBox1.Size = New System.Drawing.Size(48, 41)
        Me.PictureBox1.TabIndex = 4
        Me.PictureBox1.TabStop = False
        '
        'connectionStatus
        '
        Me.connectionStatus.Font = New System.Drawing.Font("Kelson Sans", 12.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.connectionStatus.ForeColor = System.Drawing.Color.White
        Me.connectionStatus.Location = New System.Drawing.Point(1, 83)
        Me.connectionStatus.Name = "connectionStatus"
        Me.connectionStatus.Size = New System.Drawing.Size(399, 51)
        Me.connectionStatus.TabIndex = 5
        Me.connectionStatus.Text = "Disconnected from FL Studio"
        Me.connectionStatus.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'flConnectionTimer
        '
        Me.flConnectionTimer.Interval = 1000
        '
        'Main
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(8.0!, 16.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.BackColor = System.Drawing.Color.FromArgb(CType(CType(64, Byte), Integer), CType(CType(64, Byte), Integer), CType(CType(64, Byte), Integer))
        Me.ClientSize = New System.Drawing.Size(399, 154)
        Me.Controls.Add(Me.connectionStatus)
        Me.Controls.Add(Me.MonitorOutput)
        Me.Controls.Add(Me.PictureBox1)
        Me.Controls.Add(Me.Button2)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.Name = "Main"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Me.Text = "MixAhead"
        CType(Me.PictureBox1, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)

    End Sub
    Friend WithEvents Timer1 As System.Windows.Forms.Timer
    Friend WithEvents MonitorOutput As System.Windows.Forms.ComboBox
    Friend WithEvents Button2 As System.Windows.Forms.Button
    Friend WithEvents PictureBox1 As System.Windows.Forms.PictureBox
    Friend WithEvents connectionStatus As System.Windows.Forms.Label
    Friend WithEvents flConnectionTimer As System.Windows.Forms.Timer

End Class
