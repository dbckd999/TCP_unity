using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.IO;

public class Server : MonoBehaviour
{
    public InputField PortInput;

    List<ServerClient> clients;
    List<ServerClient> disconnectList;
    //각 List들 delete지점 찾기.

    TcpListener server;
    bool serverStarted;


	public void ServerCreate()
	{
        clients = new List<ServerClient>();
        disconnectList = new List<ServerClient>();
        
        try
        {
            int port = PortInput.text == "" ? 7777 : int.Parse(PortInput.text);
            server = new TcpListener(IPAddress.Any, port);
            server.Start();

            StartListening();
            serverStarted = true;
            Chat.instance.ShowMessage($"서버가 {port}에서 시작되었습니다.");
        }
        catch (Exception e) 
        {
            Chat.instance.ShowMessage($"Socket error: {e.Message}");
        }
	}

	void Update()
	{
	    // 서버가 시작하지 않으면 종료합니다.
        if (!serverStarted) return;
        
        //모든 클라이언트들을 순회.
        foreach (ServerClient c in clients) 
        {
            // 클라이언트가 여전히 연결되있나?
            if (!IsConnected(c.tcp))
            {
                c.tcp.Close();
                disconnectList.Add(c);
                continue;
            }
            // 클라이언트로부터 체크 메시지를 받는다
            else 
            {
                NetworkStream s = c.tcp.GetStream();
                // 데이터를 읽을 수 있는가?
                if (s.DataAvailable) 
                {
                    string data = new StreamReader(s, true).ReadLine();
                    // 데이터가 비어있지 않다면 데이터 분석.
                    if (data != null)
                        OnIncomingData(c, data);
                }
                
                //실행해볼 예시. 예외사용.
                /*
                try{
                    string data = new StreamReader(s, true).ReadLine();
                    if(data != null)
                        OnIncomingData(c, data);
                }
                catch(Exception e)
                {
                    Console.writeLine(e.Message());
                }
                */
                
                
            }
        }

		for (int i = 0; i < disconnectList.Count - 1; i++)
		{
            Broadcast($"{disconnectList[i].clientName} 연결이 끊어졌습니다", clients);

            clients.Remove(disconnectList[i]);
            disconnectList.RemoveAt(i);
		}
	}

	

	bool IsConnected(TcpClient c)
	{
        try
        {
            if (c != null && c.Client != null && c.Client.Connected)
            {
                if (c.Client.Poll(0, SelectMode.SelectRead))
                    return !(c.Client.Receive(new byte[1], SocketFlags.Peek) == 0);

                return true;
            }
            else
                return false;
        }
        catch 
        {
            return false;
        }
	}

	void StartListening()
	{
	    //비동기통신 소캣생성후 연결.
        server.BeginAcceptTcpClient(AcceptTcpClient, server);
	}

    void AcceptTcpClient(IAsyncResult ar) 
    {
        //비동기통신 종료.
        TcpListener listener = (TcpListener)ar.AsyncState;
        clients.Add(new ServerClient(listener.EndAcceptTcpClient(ar)));
        /*
        list에 SericeClient형 추가
        clients.Add(new ServerClient(
        
        질문: ServieClient에 메서드 EndAcceptTcpClient반환형이 적합한가?
        답변: 
        TcpListener.EndAcceptTcpClient
        들어오는 연결 시도를 비동기적으로 받아들이고 원격 호스트 통신을 처리할 새로운 TcpClient을 만듭니다.
        반환 - TcpClient
        결론 - 적합함.
                          listener.EndAcceptTcpClient(ar)
                                                    ));
        */
        //질문: clients에 계속 Add를 실행해도 메모리에 문제가 없는가?
        StartListening();

        // 메시지를 연결된 모두에게 보냄
        Broadcast("%NAME", new List<ServerClient>() { clients[clients.Count - 1] });
    }


    void OnIncomingData(ServerClient c, string data)
    {
        // 해당 문자열이 있는지  확인
        if (data.Contains("&NAME")) 
        {
            c.clientName = data.Split('|')[1];
            Broadcast($"{c.clientName}이 연결되었습니다", clients);
            return;
        }

        Broadcast($"{c.clientName} : {data}", clients);
    }

    void Broadcast(string data, List<ServerClient> cl) 
    {
        foreach (var c in cl) 
        {
            try 
            {
                StreamWriter writer = new StreamWriter(c.tcp.GetStream());
                writer.WriteLine(data);
                writer.Flush();
            }
            catch (Exception e) 
            {
                Chat.instance.ShowMessage($"쓰기 에러 : {e.Message}를 클라이언트에게 {c.clientName}");
            }
        }
    }
}


public class ServerClient
{
    public TcpClient tcp;
    public string clientName;

    public ServerClient(TcpClient clientSocket) 
    {
        clientName = "Guest";
        tcp = clientSocket;
    }
}
