#include "TCPClient.h"
#include "Protocol.h"
#include <wx/sckaddr.h>
#include <wx/socket.h>

wxBEGIN_EVENT_TABLE(TCPClient, wxEvtHandler)
    EVT_SOCKET(wxID_ANY, TCPClient::onSocketEvent)
wxEND_EVENT_TABLE()

TCPClient::TCPClient(const wxString &address, int port)
    : m_address(address), m_port(port), m_connected(false) {
    m_client = new wxSocketClient();
    m_client->SetEventHandler(*this);
    m_client->SetNotify(wxSOCKET_CONNECTION_FLAG |
                        wxSOCKET_INPUT_FLAG |
                        wxSOCKET_LOST_FLAG);
    m_client->Notify(true);
}

bool TCPClient::connect() {
    wxIPV4address addr;
    addr.Hostname(m_address);
    addr.Service(m_port);

    if (!m_client->Connect(addr, false)) return false;
    m_connected = true;
    return true;
}

bool TCPClient::sendPinRequest(const std::string& pin) {
    UnityMessage msg;
    msg.type = UnityMessage::Type::PIN_REQUEST;
    msg.pin = pin;
    return sendMessage(msg);
}

bool TCPClient::sendMessage(const UnityMessage &msg) {
    if (!m_connected) return false;
    try {
        auto data = Protocol::serialize(msg);
        uint32_t size = static_cast<uint32_t>(data.size());

        m_client->Write(&size, sizeof(size));
        m_client->Write(data.data(), data.size());
        return !m_client->Error();
    } catch (const ProtocolError& e) {
        return false;
    }
}

void TCPClient::handleIncomingData() {
    try {
        uint32_t msgSize;
        m_client->Read(&msgSize, sizeof(msgSize));

        if (msgSize > Protocol::MAX_MESSAGE_SIZE) throw ProtocolError("message too large");

        std::vector<uint8_t> data(msgSize);
        m_client->Read(data.data(), msgSize);

        UnityMessage msg = Protocol::deserialize(data);
        if (msg.type == UnityMessage::Type::SENSOR_DATA && m_onData) {
            m_onData(msg.value);
        }
    } catch (const ProtocolError&) {
        disconnect();
    }
}

void TCPClient::onSocketEvent(wxSocketEvent &event) {
    switch(event.GetSocketEvent()) {
        case wxSOCKET_INPUT:
            handleIncomingData();
            break;
        case wxSOCKET_LOST:
            disconnect();
            break;
        default:
            break;
    }
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
