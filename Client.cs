using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Net.Sockets;
using System.IO;
using System;

public class Client : MonoBehaviour
{
	public InputField IPInput, PortInput, NickInput;
	string clientName;

    bool socketReady;
    TcpClient socket;
    NetworkStream stream;
	StreamWriter writer;
    StreamReader reader;


	public void ConnectToServer()
	{
		// 이미 연결되었다면 함수 무시
		if (socketReady) return;

		// 기본 호스트/ 포트번호
		string ip = IPInput.text == "" ? "127.0.0.1" : IPInput.text;
		int port = PortInput.text == "" ? 7777 : int.Parse(PortInput.text);

		// 소켓 생성
		try
		{
			// 소켓
			socket = new TcpClient(ip, port);
			// 스트림 생성
			stream = socket.GetStream();
			// 스트림 쓰기 생성
			writer = new StreamWriter(stream);
			// 스트림 읽기 생성
			reader = new StreamReader(stream);
			// 준비완료
			socketReady = true;
		}
		catch (Exception e) 
		{
			Chat.instance.ShowMessage($"소켓에러 : {e.Message}");
		}
	}

	void Update()
	{
		// 읽을 데이터가 있다면
		if (socketReady && stream.DataAvailable) 
		{
			// 스트림 읽기
			string data = reader.ReadLine();
			// 비어있지 않다면
			if (data != null)
				OnIncomingData(data);
		}
	}

	void OnIncomingData(string data)
	{
		if (data == "%NAME") 
		{
			clientName = NickInput.text == "" ? "Guest" + UnityEngine.Random.Range(1000, 10000) : NickInput.text;
			Send($"&NAME|{clientName}");
			return;
		}

		Chat.instance.ShowMessage(data);
	}

	void Send(string data)
	{
		if (!socketReady) return;

		// 문자열과 줄 종결자를 차례로 스트림에 씁니다.
		writer.WriteLine(data);
		// 질문: 바로 보내지는가? 스트림 처리 로직은?
		// 답변: 스트림에 쓴 후 Flush로 쓰기 스트림을 지운다. 스트림 처리는 stream변수에서 작업함.
		writer.Flush();
	}

	public void OnSendButton(InputField SendInput) 
	{
#if (UNITY_EDITOR || UNITY_STANDALONE)
		if (!Input.GetButtonDown("Submit")) return;
		SendInput.ActivateInputField();
#endif
		if (SendInput.text.Trim() == "") return;

		string message = SendInput.text;
		SendInput.text = "";
		Send(message);
	}


	void OnApplicationQuit()
	{
		CloseSocket();
	}

	void CloseSocket()
	{
		if (!socketReady) return;

		writer.Close();
		reader.Close();
		socket.Close();
		socketReady = false;
	}
}