#include "TCPClient.h"
#include "Protocol.h"
#include <wx/sckaddr.h>
#include <wx/socket.h>
#include <wx/utils.h>
#include <wx/stopwatch.h>
#include <wx/app.h>

TCPClient::TCPClient(const wxString &address, int port)
    : m_address(address), m_port(port), m_connected(false) {
    m_logFile.open("tcpclient_debug.log", std::ios::app);
    log("TCPClient constructor");

    m_client = new wxSocketClient();
    m_client->SetEventHandler(*this);
    m_client->SetNotify(wxSOCKET_CONNECTION_FLAG |
                        wxSOCKET_INPUT_FLAG |
                        wxSOCKET_LOST_FLAG);
    m_client->Notify(true);

    Bind(wxEVT_SOCKET, &TCPClient::onSocketEvent, this);
}

bool TCPClient::connect() {
    if (m_client && m_client->IsConnected()) {
        log("Already connected, disconnecting first");
        disconnect();
    }
    
    log("Attempting connection to " + m_address);
    
    wxIPV4address addr;
    addr.Hostname(m_address);
    addr.Service(m_port);
    
    m_client->Connect(addr, false);
    log("Connect initiated");
    
    return true;
}

bool TCPClient::sendPinRequest(const std::string& pin) {
    if (!m_connected || !m_handshakeComplete) {
        log("Cannot send PIN - not fully connected");
        return false;
    }

    UnityMessage msg;
    msg.type = UnityMessage::Type::PIN_REQUEST;
    msg.pin = pin;

    log("Sending PIN request: " + pin);
    return sendMessage(msg);
}

bool TCPClient::sendMessage(const UnityMessage &msg) {
    if (!m_client || !m_client->IsConnected()) {
        log("Cannot send - socket not ready");
        return false;
    }

    try {
        auto data = Protocol::serialize(msg);
        uint32_t size = static_cast<uint32_t>(data.size());

        // Write message size
        m_client->Write(&size, sizeof(size));
        if (m_client->Error() || m_client->LastCount() != sizeof(size)) {
            log("Failed to write size - bytes written: " + 
                wxString::Format("%zu", m_client->LastCount()));
            return false;
        }

        // Write message data
        m_client->Write(data.data(), data.size());
        if (m_client->Error() || m_client->LastCount() != data.size()) {
            log("Failed to write data - bytes written: " + 
                wxString::Format("%zu", m_client->LastCount()));
            return false;
        }

        log("Message sent successfully - size: " + 
            wxString::Format("%zu", data.size()));
        return true;
    } catch (const ProtocolError& e) {
        log("Protocol error: " + wxString(e.what()));
        return false;
    }
}

void TCPClient::handleIncomingData() {
    try {
        uint32_t msgSize;
        m_client->Read(&msgSize, sizeof(msgSize));
        
        std::vector<uint8_t> data(msgSize);
        m_client->Read(data.data(), msgSize);
        
        UnityMessage msg = Protocol::deserialize(data);
        log("Received message type: " + wxString::Format("%d", static_cast<int>(msg.type)));
        
        switch(msg.type) {
            case UnityMessage::Type::PIN_RESPONSE:
                log("PIN Response received: " + wxString::Format("%.1f", msg.value));
                if (msg.value > 0) {
                    m_connected = true;
                    log("Sensor connection confirmed");
                } else {
                    log("PIN rejected");
                    disconnect();
                }
                break;
                
            case UnityMessage::Type::SENSOR_DATA:
                if (m_onData) m_onData(msg.value);
                break;
        }
    } catch (const ProtocolError& e) {
        log("Protocol error: " + wxString(e.what()));
        disconnect();
    }
}

void TCPClient::onSocketEvent(wxSocketEvent &event) {
    switch(event.GetSocketEvent()) {
        case wxSOCKET_CONNECTION:
            log("Connection established at socket level");
            if (m_client->IsConnected()) {
                wxMilliSleep(100);
                UnityMessage msg;
                msg.type = UnityMessage::Type::CONNECT;
                if (sendMessage(msg)) {
                    log("Handshake sent successfully");
                    // Don't set m_connected yet, wait for response
                }
            }
            break;
            
        case wxSOCKET_INPUT:
            if (m_client->IsConnected()) {
                log("Processing incoming data");
                try {
                    handleIncomingData();
                    if (!m_handshakeComplete) {
                        m_connected = true;
                        m_handshakeComplete = true;
                        log("Connection fully established");
                    }
                } catch (const ProtocolError& e) {
                    log("Protocol error: " + wxString(e.what()));
                    disconnect();
                }
            }
            break;
            
        case wxSOCKET_LOST:
            log("Connection lost");
            m_connected = false;
            m_handshakeComplete = false;
            break;
    }
}

bool TCPClient::waitForConnection(int timeout) {
    // Add stopwatch for timeout tracking
    wxStopWatch sw;
    m_handshakeComplete = false; // reset state
    
    // Allow events while waiting
    while (!m_handshakeComplete && sw.Time() < timeout * 1000) {
        wxMilliSleep(100);
        if (wxTheApp) wxTheApp->Yield();
    }
    
    log("Wait completed: " + wxString::Format("%ld ms", sw.Time()));
    return m_handshakeComplete;
}

void TCPClient::disconnect() {
    if (m_client && m_connected) {
        m_client->Close();
        m_connected = false;
    }
}

TCPClient::~TCPClient() {
    disconnect();
    delete m_client;
}

void TCPClient::log(const wxString& message) {
    if (m_logFile.is_open()) {
    m_logFile << wxDateTime::Now().Format("%Y-%m-%d %H:%M:%S: ").ToStdString()
            << message.ToStdString() << std::endl;
    m_logFile.flush();
    }
}
