#pragma once
#include <fstream>
#include <wx/event.h>
#include <wx/socket.h>
#include "Protocol.h"

class TCPClient : public wxEvtHandler {
public:
    TCPClient(const wxString& address = "172.27.147.202", int port = 8080);
    ~TCPClient();

    bool connect();
    void disconnect();
    bool waitForConnection(int timeout = 5);
    bool isConnected() const { return m_connected; }
    bool sendPinRequest(const std::string& pin);

    using DataCallback = std::function<void(float)>;
    void setDataCallback(DataCallback callback) { m_onData = callback; }

private:
    wxSocketClient *m_client;
    wxString m_address;
    int m_port;
    bool m_connected;
    bool m_handshakeComplete;
    DataCallback m_onData;

    void onSocketEvent(wxSocketEvent &event);
    bool sendMessage(const UnityMessage &msg);
    void handleIncomingData();

    std::ofstream m_logFile;
    void log(const wxString& msg);
};
