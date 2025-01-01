#pragma once
#include <string>
#include <vector>

class Sensor {
private:
    std::string m_pin;
    bool m_connected;
    float m_lastValue;
    std::vector<float> m_history;

public:
    explicit Sensor(const std::string& pin);

    std::string getPin() { return m_pin; }
    bool isConnected() { return m_connected; }
    float getLastValue() const { return m_lastValue; }
    const std::vector<float>& getHistory() const { return m_history; }

    bool connect();
    void disconnect();

    void updateValue(float value);
};
