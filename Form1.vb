Imports System.Net
Imports System.Threading

Public Class Form1
    Private socketListener As Sockets.TcpListener
    Private socketClient As ClientTcpIp() = New ClientTcpIp(3) {}
    Private WithEvents txt As TextBox

    Private Sub Form1_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load
        Me.SuspendLayout()
        Me.Width = 332
        Me.Height = 180
        txt = New TextBox
        Me.Controls.Add(txt)
        txt.Left = 10
        txt.Top = 10
        txt.Multiline = True
        txt.Width = 300
        txt.Height = 129
        Me.ResumeLayout()

        Dim socketPort As Integer = 8888
        Dim endPoint As New IPEndPoint(IPAddress.Any, socketPort)
        socketListener = New Sockets.TcpListener(endPoint)
        socketListener.Start()
        Dim myServerThread As New Thread(New ThreadStart(AddressOf ServerThread))
        myServerThread.Start()
    End Sub

    Private Sub Form_Closed(ByVal sender As Object, ByVal e As System.Windows.Forms.FormClosedEventArgs) Handles Me.FormClosed
        socketListener.Stop()
        For i As Integer = 0 To socketClient.GetLength(0) - 1
            If socketClient(i) Is Nothing = False AndAlso socketClient(i).isConnected = True Then
                socketClient(i).Close()
            End If
        Next
    End Sub

    Private Sub ServerThread()
        Try
            Dim i As Integer
            While True
                Dim tcpClient As Sockets.TcpClient = socketListener.AcceptTcpClient()
                For i = 0 To socketClient.GetLength(0) - 1
                    If socketClient(i) Is Nothing Then
                        Exit For
                    ElseIf socketClient(i).isConnected = False Then
                        Exit For
                    End If
                Next
                If i < socketClient.GetLength(0) Then
                    socketClient(i) = New ClientTcpIp(tcpClient, Me)
                    Dim clientThread As New Thread(New ThreadStart(AddressOf socketClient(i).ReadWrite))
                    clientThread.Start()
                Else
                    tcpClient.Close()
                End If
            End While
        Catch ex As Exception
        End Try
    End Sub

    Friend Sub SetTextBox1(ByVal str As String)
        txt.Text = str
    End Sub

    Public Class ClientTcpIp
        Delegate Sub SetTextBox1Delegate(ByVal str As String)
        Private TextBox1Delegate As SetTextBox1Delegate
        Private _Form1 As Form1
        Private objSck As Sockets.TcpClient
        Private objStm As Sockets.NetworkStream

        Public Sub New(ByVal tcp As Sockets.TcpClient, ByVal frm As Form)
            _Form1 = CType(frm, Form1)
            TextBox1Delegate = New SetTextBox1Delegate(AddressOf _Form1.SetTextBox1)
            objSck = tcp
            objStm = tcp.GetStream()
        End Sub

        Public Sub ReadWrite()
            Try
                While True
                    Dim rdat As Byte() = New Byte(1023) {}
                    Dim ldat As Int32 = objStm.Read(rdat, 0, rdat.GetLength(0))
                    If ldat > 0 Then
                        Dim sdat As Byte() = New Byte(ldat - 1) {}
                        Array.Copy(rdat, sdat, ldat)
                        Dim rMsg As String = System.Text.Encoding.UTF8.GetString(sdat)
                        rMsg.Trim()
                        If (rMsg.StartsWith("<policy-file-request/>")) Then
                            Console.WriteLine("policy file request.")
                            Dim msg As String = "<cross-domain-policy><allow-access-from domain=""*"" to-ports=""8888"" /></cross-domain-policy>" & vbNullChar
                            sdat = System.Text.Encoding.UTF8.GetBytes(msg)
                            objStm.Write(sdat, 0, sdat.GetLength(0))
                            Close()
                        Else
                            Console.WriteLine(rMsg)
                            _Form1.Invoke(TextBox1Delegate, rMsg)
                        End If
                    Else
                        Close()
                    End If
                End While
            Catch ex As Exception
            End Try
        End Sub

        Public Sub Close()
            objStm.Close()
            objSck.Close()
        End Sub

        Public Function isConnected() As Boolean
            isConnected = objSck.Connected
        End Function
    End Class
End Class
