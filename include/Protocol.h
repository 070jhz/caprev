#pragma once
#include <cstdint>
#include <string>
#include <vector>
#include <stdexcept>

class ProtocolError : public std::runtime_error {
public:
    explicit ProtocolError(const std::string& msg) : std::runtime_error(msg) {}
};

struct UnityMessage {
    enum class Type {
        CONNECT,        // initial connection request
        PIN_REQUEST,    // app requests access to sensor 
        PIN_RESPONSE,   // unity confirms or rejects
        SENSOR_DATA,    // unity sends sensor reading
        ERROR_STATE     // error condition
    };

    Type type;
    std::string pin;
    float value;
    std::string error;
};

namespace Protocol {
    std::vector<uint8_t> serialize(const UnityMessage& msg) ;
    UnityMessage deserialize(const std::vector<uint8_t>& data);
    constexpr uint32_t PROTOCOL_VERSION = 1;
    constexpr size_t MAX_MESSAGE_SIZE = 1024;
};


