#pragma once
#include <wx/event.h>
#include <wx/socket.h>
#include "Protocol.h"

class TCPClient : public wxEvtHandler {
public:
    TCPClient(const wxString& address = "localhost", int port = 8080);
    ~TCPClient();

    bool connect();
    void disconnect();
    bool isConnected() const { return m_connected; }
    bool sendPinRequest(const std::string& pin);

    using DataCallback = std::function<void(float)>;
    void setDataCallback(DataCallback callback) { m_onData = callback; }

private:
    wxSocketClient *m_client;
    wxString m_address;
    int m_port;
    bool m_connected;
    DataCallback m_onData;

    void onSocketEvent(wxSocketEvent &event);
    bool sendMessage(const UnityMessage &msg);
    void handleIncomingData();

    wxDECLARE_EVENT_TABLE();
};
