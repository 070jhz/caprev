#include "Sensor.h"
#include <random>

Sensor::Sensor(const std::string& pin) :
    m_pin(pin),
    m_connected(false),
    m_lastValue(0.0f) {}

bool Sensor::connect() {
    m_connected = true; // to be replaced with actual connection logic
    return m_connected;
}

void Sensor::disconnect() {
    m_connected = false; // same
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
