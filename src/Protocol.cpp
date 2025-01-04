#include "../include/Protocol.h"
#include <cstdint>
#include <cstring>

namespace Protocol {

std::vector<uint8_t> serialize(const UnityMessage& msg) {
    std::vector<uint8_t> buf;
    buf.reserve(MAX_MESSAGE_SIZE);
    
    // version
    buf.push_back(static_cast<uint8_t>(PROTOCOL_VERSION));

    // type
    buf.push_back(static_cast<uint8_t>(msg.type));

    // pin length and data
    buf.push_back(static_cast<uint8_t>(msg.pin.length()));
    buf.insert(buf.end(), msg.pin.begin(), msg.pin.end());
    
    // value
    const uint8_t* valuePtr = reinterpret_cast<const uint8_t*>(&msg.value);
    buf.insert(buf.end(), valuePtr, valuePtr + sizeof(float));
    
    // error if present
    if (msg.type == UnityMessage::Type::ERROR_STATE) {
        uint8_t errorLen = static_cast<uint8_t>(msg.error.length());
        buf.push_back(errorLen);
        buf.insert(buf.end(), msg.error.begin(), msg.error.end());
    }

    return buf;
}

UnityMessage deserialize(const std::vector<uint8_t>& data) {
    if (data.size() < 3) throw ProtocolError("message too short");

    UnityMessage msg;
    size_t offset = 0;

    // verify protocol version
    if (data[offset++] != PROTOCOL_VERSION) {
        throw ProtocolError("protocol version mismatch");
    }

    // read type
    msg.type = static_cast<UnityMessage::Type>(data[offset++]);
    
    // read pin
    uint8_t pinLen = data[offset++];
    if (offset + pinLen > data.size()) throw ProtocolError("invalid pin length");
    msg.pin.assign(data.begin() + offset, data.begin() + offset + pinLen);
    offset += pinLen;

    // read value
    if (offset + sizeof(float) > data.size()) throw ProtocolError("message truncated at value");
    std::memcpy(&msg.value, &data[offset], sizeof(float));
    offset += sizeof(float);

    // read error if present
    if (msg.type == UnityMessage::Type::ERROR_STATE && offset < data.size()) {
        uint8_t errorLen = data[offset++];
        if (offset + errorLen <= data.size()) {
            msg.error.assign(data.begin() + offset, data.begin() + offset + errorLen);
        }
    }

    return msg;
}

} // namespace Protocol
