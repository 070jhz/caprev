#include "Sensor.h"

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
    m_history.push_back(value);
}

