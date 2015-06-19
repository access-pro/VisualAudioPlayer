Imports System
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.Data
Imports System.Drawing
Imports System.Text
Imports System.Windows.Forms
Imports System.Runtime.InteropServices


Public Class frmMain
    Inherits Form
    <DllImport("bass.dll")> _
    Public Shared Function BASS_Init(ByVal device As Integer, ByVal freq As UInteger, ByVal flag As UInteger, ByVal hParent As IntPtr, ByVal GUID As UInteger) As Boolean
    End Function

    <DllImport("bass.dll")> _
    Public Shared Function BASS_StreamCreateFile(ByVal mem As Boolean, <MarshalAs(UnmanagedType.LPWStr)> ByVal str As [String], ByVal offset As Long, ByVal length As Long, ByVal flags As Long) As Integer
    End Function

    <DllImport("bass.dll")> _
    Public Shared Function BASS_ErrorGetCode() As UInteger
    End Function

    <DllImport("bass.dll")> _
    Public Shared Function BASS_Free() As Boolean
    End Function

    <DllImport("bass.dll")> _
    Public Shared Function BASS_StreamFree(ByVal stream As Integer) As Boolean
    End Function

    <DllImport("bass.dll")> _
    Public Shared Function BASS_ChannelPlay(ByVal stream As Integer, ByVal restart As Boolean) As Boolean
    End Function

    <DllImport("bass.dll")> _
    Public Shared Function BASS_ChannelStop(ByVal stream As Integer) As Boolean
    End Function

    <DllImport("bass_sfx.dll")> _
    Public Shared Function BASS_SFX_Init(ByVal hInstance As IntPtr, ByVal hWnd As IntPtr) As Boolean
    End Function

    <DllImport("bass_sfx.dll")> _
    Public Shared Function BASS_SFX_PluginCreate(ByVal file As String, ByVal hPluginWnd As IntPtr, ByVal width As Integer, ByVal height As Integer, ByVal flags As Integer) As Integer
    End Function

    <DllImport("bass_sfx.dll")> _
    Public Shared Function BASS_SFX_PluginStart(ByVal handle As Integer) As Boolean
    End Function

    <DllImport("bass_sfx.dll")> _
    Public Shared Function BASS_SFX_PluginSetStream(ByVal handle As Integer, ByVal stream As Integer) As Boolean
    End Function

    <DllImport("bass_sfx.dll")> _
    Public Shared Function BASS_SFX_PluginRender(ByVal handle As Integer, ByVal hStream As Integer, ByVal hDC As IntPtr) As IntPtr
    End Function

    Private Declare Function GetDC Lib "user32.dll" (ByVal hWnd As Int32) As IntPtr

    Private Declare Function ReleaseDC Lib "user32.dll" (ByVal hWnd As Int32, ByVal hDC As IntPtr) As Int32

    Private hStream As Integer = 0
    Private BASS_UNICODE As Integer = -2147483648

    Private hSFX As Integer = 0
    Private hSFX2 As Integer = 0
    Private hSFX3 As Integer = 0
    Private hSFX4 As Integer = 0

    Private hVisDC As IntPtr = IntPtr.Zero
    Private hVisDC2 As IntPtr = IntPtr.Zero
    Private hVisDC3 As IntPtr = IntPtr.Zero

    Private Sub frmMain_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load
        hVisDC = GetDC(m_oVisPanel.Handle)
        hVisDC2 = GetDC(m_oVisPanel2.Handle)
        hVisDC3 = GetDC(m_oVisPanel3.Handle)

        If BASS_Init(-1, 44100, 0, Me.Handle, 0) Then
            BASS_SFX_Init(System.Diagnostics.Process.GetCurrentProcess().Handle, Me.Handle)

            hStream = BASS_StreamCreateFile(False, "music\Matrix.mp3", 0, 0, BASS_UNICODE)
            BASS_ChannelPlay(hStream, False)

            hSFX = BASS_SFX_PluginCreate("plugins\sphere.svp", m_oVisPanel.Handle, m_oVisPanel.Width, m_oVisPanel.Height, 0)
            'sonique
            hSFX2 = BASS_SFX_PluginCreate("plugins\blaze.dll", m_oVisPanel2.Handle, m_oVisPanel2.Width, m_oVisPanel2.Height, 0)
            'windows media player
            hSFX3 = BASS_SFX_PluginCreate("BBPlugin\oscillo.dll", m_oVisPanel3.Handle, m_oVisPanel3.Width, m_oVisPanel3.Height, 0)
            hSFX4 = BASS_SFX_PluginCreate("plugins\vis_milk2.dll", IntPtr.Zero, 0, 0, 0)

            BASS_SFX_PluginSetStream(hSFX4, hStream)
            'bassbox
            BASS_SFX_PluginStart(hSFX)
            BASS_SFX_PluginStart(hSFX2)
            BASS_SFX_PluginStart(hSFX3)
            BASS_SFX_PluginStart(hSFX4)
            timer1.Interval = 27
            timer1.Enabled = True
        End If
    End Sub

    Private Sub Timer1_Tick(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Timer1.Tick
        If hSFX <> -1 Then
            BASS_SFX_PluginRender(hSFX, hStream, hVisDC)
        End If
        If hSFX2 <> -1 Then
            BASS_SFX_PluginRender(hSFX2, hStream, hVisDC2)
        End If
        If hSFX3 <> -1 Then
            BASS_SFX_PluginRender(hSFX3, hStream, hVisDC3)
        End If
    End Sub

    Private Sub frmMain_FormClosed(ByVal sender As System.Object, ByVal e As System.Windows.Forms.FormClosedEventArgs) Handles MyBase.FormClosed
        ReleaseDC(m_oVisPanel.Handle, hVisDC)
        ReleaseDC(m_oVisPanel2.Handle, hVisDC2)
        ReleaseDC(m_oVisPanel3.Handle, hVisDC3)
    End Sub
End Class
