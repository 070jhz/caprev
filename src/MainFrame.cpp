#include "MainFrame.h"
#include <memory>
#include <string>
#include <wx/event.h>
#include <wx/gdicmn.h>
#include <wx/msgdlg.h>
#include <wx/msw/button.h>
#include <wx/msw/stattext.h>
#include <wx/stattext.h>
#include <wx/textctrl.h>
#include <wx/tglbtn.h>
#include <wx/timer.h>
#include <wx/wx.h>

MainFrame::MainFrame() : m_selectedSensor(-1),
    wxFrame(nullptr, wxID_ANY, "Sensor Monitor", wxDefaultPosition, wxSize(800,600)) {
    
    m_logFile.open("debugmf.log", std::ios::app);

    log("mainframe construction start");
    CreateStatusBar();
    // create main panels
    m_leftPanel = new wxPanel(this);
    m_rightPanel = new wxPanel(this);
    m_rightPanel->SetBackgroundColour(wxColour(240,240,240));

    // main horizontal layout 
    auto mainSizer = new wxBoxSizer(wxHORIZONTAL);

    // left panel components
    auto leftSizer = new wxBoxSizer(wxVERTICAL);
    m_pinInput = new wxTextCtrl(m_leftPanel, wxID_ANY, "",
                                wxDefaultPosition, wxDefaultSize,
                                wxTE_PROCESS_ENTER);
    m_connectBtn = new wxButton(m_leftPanel, wxID_ANY, "Connect");
    m_sensorList = new wxListBox(m_leftPanel, wxID_ANY);
    m_connectBtn->Bind(wxEVT_BUTTON, &MainFrame::onConnect, this);
    m_pinInput->Bind(wxEVT_TEXT_ENTER, &MainFrame::onConnect,this);
    m_sensorList->Bind(wxEVT_LISTBOX, &MainFrame::onSensorSelected, this);
    
    leftSizer->Add(new wxStaticText(m_leftPanel, wxID_ANY, "Enter Sensor PIN:"),
                   0, wxALL, 5);
    leftSizer->Add(m_pinInput, 0, wxEXPAND | wxALL, 5);
    leftSizer->Add(m_connectBtn, 0, wxEXPAND | wxALL, 5);
    leftSizer->Add(new wxStaticText(m_leftPanel, wxID_ANY, "Connected Sensors:"),
                   0, wxTOP | wxLEFT, 10);
    leftSizer->Add(m_sensorList, 1, wxEXPAND | wxALL, 5);

    m_leftPanel->SetSizer(leftSizer);

    // right panel components
    auto rightSizer = new wxBoxSizer(wxVERTICAL);
    m_valueDisplay = new wxStaticText(m_rightPanel, wxID_ANY, "No sensor data",
                                      wxDefaultPosition, wxDefaultSize,
                                      wxALIGN_CENTER | wxST_NO_AUTORESIZE);
    m_valueDisplay->SetFont(m_valueDisplay->GetFont().Scale(1.5));
    m_graphPanel = new GraphPanel(m_rightPanel);
    m_graphPanel->SetMinSize(wxSize(300, 200));
    m_recordBtn = new wxToggleButton(m_rightPanel, wxID_ANY, "Record");
    m_recordBtn->Bind(wxEVT_TOGGLEBUTTON, &MainFrame::onRecordToggle, this);

    rightSizer->Add(m_valueDisplay, 0, wxEXPAND | wxALL, 10);
    rightSizer->Add(m_recordBtn,0, wxEXPAND | wxALL, 5);
    rightSizer->Add(m_graphPanel, 1, wxEXPAND | wxALL, 5);
    m_rightPanel->SetSizer(rightSizer);
    
    // add panels to main layout
    mainSizer->Add(m_leftPanel, 3, wxEXPAND | wxALL, 5);
    mainSizer->Add(m_rightPanel, 7, wxEXPAND | wxALL);
    SetSizer(mainSizer);
    
    Bind(wxEVT_THREAD, &MainFrame::onSensorUpdate, this);
    m_updateTimer = new wxTimer(this);
    Bind(wxEVT_TIMER, &MainFrame::onTimer, this);
    m_updateTimer->Start(TIMER_INTERVAL);
}

void MainFrame::log(const wxString& message) {
    if (m_logFile.is_open()) {
        m_logFile << wxDateTime::Now().Format("%Y-%m-%d %H:%M:%S: ").ToStdString()
                << message.ToStdString() << std::endl;
        m_logFile.flush();
    }
}

void MainFrame::onRecordToggle(wxCommandEvent &event) {
    m_isRecording = m_recordBtn->GetValue();

    if (m_isRecording) {
        if (m_selectedSensor < 0 || m_selectedSensor >= m_sensors.size()) {
            wxMessageBox("Please select a sensor first", "Warning", wxOK | wxICON_WARNING);
            m_recordBtn->SetValue(false);
            m_isRecording = false;
            return;
        }
        m_graphPanel->clear();
        m_graphPanel->resetTime();
    }
}

void MainFrame::onSensorSelected(wxCommandEvent &event) {
    m_selectedSensor = m_sensorList->GetSelection();
    updateDisplay();
}

void MainFrame::onTimer(wxTimerEvent &event) {
    checkServerConnection();

    if (!isServerConnected()) {
        m_valueDisplay->SetLabel("Server disconnected");
        m_recordBtn->SetValue(false);
        m_isRecording = false;
        return;
    }
 
    updateDisplay();
}

void MainFrame::updateDisplay() {
    log("updateDisplay called");
    if (m_sensors.empty()) {
        log("No sensors");
        m_valueDisplay->SetLabel("No active sensors");
        return;
    }
    
    if (m_selectedSensor >= 0 && m_selectedSensor < m_sensors.size()) {
        auto& sensor = m_sensors[m_selectedSensor];
        if (sensor && sensor->isConnected()) {
            wxString value = wxString::Format("Sensor %s - Last value: %.2f",
                                              sensor->getPin(),
                                              sensor->getLastValue());
            log("Updating display: " + value.ToStdString());
            m_valueDisplay->SetLabel(value);
        }
    } else {
        m_valueDisplay->SetLabel("Select a sensor to view data");
    }

    m_rightPanel->Layout();
}

void MainFrame::onExit(wxCommandEvent &event) { Close(true); }

void MainFrame::onAbout(wxCommandEvent &event) {
    wxMessageBox("Caprev Companion App \nVR Sensor Monitor", "About Caprev",
                 wxOK | wxICON_INFORMATION);
}

void MainFrame::onConnect(wxCommandEvent &event) {
    try {
        wxString pin = m_pinInput->GetValue().Trim();
        if (pin.IsEmpty()) {
            wxMessageBox("Please enter a PIN", "Error");
            return;
        }

        std::string pinStr = pin.ToStdString();
        
        // check if sensor already exists
        auto it = std::find_if(m_sensors.begin(), m_sensors.end(),
            [&pinStr](const auto& sensor) { 
                return sensor->getPin() == pinStr; 
            });

        if (it != m_sensors.end()) {
            wxMessageBox("Sensor already connected", "Error");
            return;
        }

        // create new client for this sensor
        auto client = std::make_unique<TCPClient>();
        TCPClient* clientPtr = client.get();

        clientPtr->setDataCallback([this, pinStr](float value) {
            wxThreadEvent* evt = new wxThreadEvent(wxEVT_THREAD);
            evt->SetString(pinStr);
            evt->SetPayload(value);
            wxQueueEvent(this, evt);
        });


        if (!clientPtr->connect() || !clientPtr->waitForConnection(10)) {
            wxMessageBox("Failed to connect to Unity", "Error");
            return;
        }

        if (!clientPtr->sendPinRequest(pinStr)) {
            wxMessageBox("Failed to connect to sensor", "Error");
            return;
        }

        m_clients.push_back(std::move(client));
        m_pinInput->Clear();

    } catch (const std::exception& e) {
        wxMessageBox(e.what(), "Error");
    }
}

void MainFrame::onSensorUpdate(wxThreadEvent& evt)
{
   std::string pin = evt.GetString().ToStdString();
    float value = evt.GetPayload<float>();
    
    if (value == -1.0f) {  // Error state
        wxMessageBox("Invalid PIN", "Error");
        return;
    }
    
    if (value == 1000.0f) {  // PIN_RESPONSE success
        auto it = std::find_if(m_sensors.begin(), m_sensors.end(),
            [&pin](const auto& sensor) { 
                return sensor->getPin() == pin; 
            });
            
        if (it == m_sensors.end()) {
            auto sensor = std::make_unique<Sensor>(pin);
            sensor->setConnected(true);
            m_sensorList->Append(wxString(pin));
            m_sensors.push_back(std::move(sensor));
        }
        return;
    }
    
    onSensorData(value, pin);
}

void MainFrame::onSensorData(float value, const std::string &pin) {
    auto it = std::find_if(m_sensors.begin(), m_sensors.end(),
        [&pin](const auto& sensor) { 
            return sensor->getPin() == pin; 
        });
        
    if (it != m_sensors.end()) {
        (*it)->updateValue(value);
        
        if (m_selectedSensor >= 0 && 
            m_selectedSensor < m_sensors.size() && 
            m_sensors[m_selectedSensor]->getPin() == pin && 
            m_isRecording) {
            m_graphPanel->addPoint(value);
        }
        updateDisplay();
    }
}

void MainFrame::checkServerConnection() {
    m_clients.erase(
        std::remove_if(m_clients.begin(), m_clients.end(),
            [](const auto& client) { return !client->isConnected(); }),
        m_clients.end());
}

bool MainFrame::isServerConnected() const {
    return std::any_of(m_clients.begin(), m_clients.end(), 
                       [](const auto& client) { return client->isConnected(); });
}

