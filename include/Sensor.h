#pragma once
#include <string>
#include <deque>
#include <memory>
#include "TCPClient.h"

class Sensor {
private:
    std::string m_pin;
    bool m_connected;
    float m_lastValue;
    std::deque<float> m_history;
    std::shared_ptr<TCPClient> m_client;

public:
    explicit Sensor(const std::string& pin);

    std::string getPin() { return m_pin; }
    void setConnected(bool state) { m_connected = state; }
    bool isConnected() { return m_connected; }
    float getLastValue() const { return m_lastValue; }
    const std::deque<float> &getHistory() const { return m_history; }
    void clearValue() {
        m_lastValue = 0.0f;
        m_connected = false;
    }

    bool connect();
    void disconnect();

    void updateValue(float value);

    // debug-only
    static constexpr size_t MAX_HISTORY = 100;
    static constexpr float MIN_VALUE = 0.0f;
    static constexpr float MAX_VALUE = 100.0f;

    float generateRandomFloat() const;
    void generateTestData();
};
