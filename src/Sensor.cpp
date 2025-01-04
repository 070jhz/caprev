#include "Sensor.h"
#include "TCPClient.h"
#include <memory>
#include <random>

Sensor::Sensor(const std::string& pin) :
    m_pin(pin),
    m_connected(false),
    m_lastValue(0.0f) {
    m_client = std::make_shared<TCPClient>();
    m_client->setDataCallback([this](float value) {
        updateValue(value);
    });
}

bool Sensor::connect() {
    if (!m_client->connect()) return false;
    if (!m_client->sendPinRequest(m_pin)) return false;
    m_connected = true;
    return true;
}

void Sensor::disconnect() {
    if (m_client) {
        m_client->disconnect();
    }
}

void Sensor::updateValue(float value) {
    m_lastValue = value;
    if (m_history.size() >= MAX_HISTORY) {
        m_history.pop_front();
    }
    m_history.push_back(value);
}

void Sensor::generateTestData() {
    updateValue(generateRandomFloat());
}

float Sensor::generateRandomFloat() const {
    static std::random_device rd;
    static std::mt19937 gen(rd());
    std::uniform_real_distribution<float> dis(MIN_VALUE, MAX_VALUE);
    return dis(gen);
}
