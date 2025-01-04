#include <fstream>
#include <wx/log.h>
#include <wx/msgdlg.h>
#include <wx/sckaddr.h>
#include <wx/wx.h>
#include <wx/socket.h>
#include <wx/datetime.h>
#include "../include/Protocol.h"

class TestServer : public wxApp {
private:
    wxSocketServer *m_server;
    wxSocketBase* m_activeClient;

    void processClientMessage(wxSocketBase* client) {
        if (!client) return;

        uint32_t msgSize;
        client->Read(&msgSize, sizeof(msgSize));

        if (msgSize > Protocol::MAX_MESSAGE_SIZE) {
            log("Message too large");
            return;
        }

        std::vector<uint8_t> data(msgSize);
        client->Read(data.data(), msgSize);

        try {
            UnityMessage msg = Protocol::deserialize(data);
            log(wxString::Format("Received message type: %d", static_cast<int>(msg.type)));
            if (msg.type == UnityMessage::Type::CONNECT) {
                UnityMessage response;
                response.type = UnityMessage::Type::PIN_RESPONSE;
                response.value = 1.0f;  // Success
            
                auto responseData = Protocol::serialize(response);
                uint32_t responseSize = responseData.size();
            
                client->Write(&responseSize, sizeof(responseSize));
                client->Write(responseData.data(), responseData.size());
                log("Sent connection confirmation");
            }
        } catch (const ProtocolError& e) {
            log("Protocol error: " + wxString(e.what()));
        }
}
    // logging for debug
    std::ofstream m_logFile;
    void log(const wxString& message) {
        if (m_logFile.is_open()) {
        m_logFile << wxDateTime::Now().Format("%Y-%m-%d %H:%M:%S: ").ToStdString()
                << message.ToStdString() << std::endl;
        m_logFile.flush();
    }
}
public:
    bool OnInit() override {
        if (!wxApp::OnInit()) return false;
        m_logFile.open("debugts.log", std::ios::app);
        wxIPV4address addr;
        addr.AnyAddress();
        addr.Service(8080);

        m_server = new wxSocketServer(addr);
        if (!m_server->Ok()) {
            wxMessageBox("failed to start server", "Error");
            return false;
        }

        m_server->SetEventHandler(*this);
        m_server->SetNotify(wxSOCKET_CONNECTION_FLAG);
        m_server->Notify(true);
        
        Bind(wxEVT_SOCKET, &TestServer::onSocketEvent, this);

        wxLogMessage("Server started on port 8080");
        return true;
    }

    void onSocketEvent(wxSocketEvent& event) {
        wxSocketBase* client = nullptr;
        log("Socket event received: ");
        switch(event.GetSocketEvent()) {
            case wxSOCKET_CONNECTION:
                log("Connection request\n");
                client = m_server->Accept(false);
                if (client) {
                    log("Client connected - waiting for handshake\n");
                    client->SetEventHandler(*this);
                    client->SetNotify(wxSOCKET_LOST_FLAG | wxSOCKET_INPUT_FLAG);
                    client->Notify(true);
                    m_activeClient = client;
                }
                break;

            case wxSOCKET_INPUT:
                log("Input received - processing message\n");
                processClientMessage(event.GetSocket());
                break;

            case wxSOCKET_LOST:
                log("Client disconnected\n");
                m_activeClient = nullptr;
                break;
            case wxSOCKET_OUTPUT:
                log("ready for output");
                break;
            default:
                 log("unknown socket event");
                break;
        }
    }

    int OnExit() override {
        if (m_server) {
            delete m_server;
        }
        return 0;
    }
};

wxIMPLEMENT_APP(TestServer);

