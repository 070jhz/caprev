#include "MainFrame.h"
#include <algorithm>
#include <memory>
#include <wx/event.h>
#include <wx/gdicmn.h>
#include <wx/msw/button.h>
#include <wx/msw/stattext.h>
#include <wx/stattext.h>
#include <wx/textctrl.h>
#include <wx/timer.h>

MainFrame::MainFrame() : m_selectedSensor(-1),
    wxFrame(nullptr, wxID_ANY, "Sensor Monitor", wxDefaultPosition, wxSize(800,600)) {
    
    m_logFile.open("debug.log", std::ios::app);

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

    rightSizer->Add(m_valueDisplay, 0, wxEXPAND | wxALL, 10);
    m_rightPanel->SetSizer(rightSizer);
    
    // add panels to main layout
    mainSizer->Add(m_leftPanel, 3, wxEXPAND | wxALL, 5);
    mainSizer->Add(m_rightPanel, 7, wxEXPAND | wxALL);
    SetSizer(mainSizer);


    // timer setup
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

void MainFrame::onSensorSelected(wxCommandEvent &event) {
    m_selectedSensor = m_sensorList->GetSelection();
    log("selected sensor " + m_selectedSensor);
    updateDisplay();
}

void MainFrame::onTimer(wxTimerEvent &event) {
    for (auto& sensor : m_sensors) {
        if (sensor && sensor->isConnected()) {
            sensor->generateTestData();
        }
    }

    updateDisplay();
}

void MainFrame::updateDisplay() {
    if (m_sensors.empty()) {
        m_valueDisplay->SetLabel("No active sensors");
        return;
    }
    
    if (m_selectedSensor >= 0 && m_selectedSensor < m_sensors.size()) {
        auto& sensor = m_sensors[m_selectedSensor];
        if (sensor && sensor->isConnected()) {
            wxString value = wxString::Format("Sensor %s - Last value: %.2f",
                                              sensor->getPin(),
                                              sensor->getLastValue());
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
        log("onConnect called");
        wxString pin = m_pinInput->GetValue().Trim();
        if (pin.IsEmpty()) {
            log("empty pin detected");
            wxMessageBox("Please enter a PIN", "Error", wxOK | wxICON_ERROR);
            return;
        }

        // check if sensor already exists
        std::string pinStr = pin.ToStdString();
        log("pin value : " + pin);

        auto it = std::find_if(m_sensors.begin(), m_sensors.end(),
                            [&pinStr](const auto& sensor) { return sensor->getPin() == pinStr; });

        if (it == m_sensors.end()) {
            // create new sensor
            log("no sensor found with the pin provided, creating new sensor");
            auto sensor = std::make_unique<Sensor>(pinStr);
            if (sensor->connect()) {
                m_sensors.push_back(std::move(sensor));
                m_sensorList->Append(pin);
                SetStatusText("Connected to sensor " + pin);
            }
        } else {
            wxMessageBox("Sensor already connected", "Warning",
                        wxOK | wxICON_WARNING);
        }

        m_pinInput->Clear();
    } catch (const std::exception& e) {
        wxMessageBox(e.what(), "Error", wxOK | wxICON_ERROR);
    }
}



