#pragma once

#include <memory>
#include <vector>
#include <fstream>
#include <wx/event.h>
#include <wx/wx.h>
#include <wx/tglbtn.h>

#include "GraphPanel.h"
#include "Sensor.h"

// main window class

class MainFrame : public wxFrame {
public:
    MainFrame();
    virtual ~MainFrame() = default;

private:
    // event handlers
    void onConnect(wxCommandEvent &event);
    void onTimer(wxTimerEvent &event);
    void onExit(wxCommandEvent &event);
    void onAbout(wxCommandEvent &event);
    void onConnectionStatus(bool connected);
    void onRecordToggle(wxCommandEvent &event);
    void updateDisplay();

    // gui elements
    wxMenuBar *m_menuBar;
    wxStatusBar *m_statusBar;
    wxPanel *m_leftPanel; // sensor connection
    wxPanel *m_rightPanel; // sensor data
    wxTextCtrl *m_pinInput;
    wxButton *m_connectBtn;
    wxListBox *m_sensorList;
    wxTimer *m_updateTimer;
    wxStaticText *m_valueDisplay;
    static constexpr int TIMER_INTERVAL = 1000;

    // sensor management
    std::vector<std::unique_ptr<Sensor>> m_sensors;
    int m_selectedSensor;
    
    // graph stuff
    GraphPanel *m_graphPanel;
    wxToggleButton *m_recordBtn;
    bool m_isRecording;

    // network
    void onSensorSelected(wxCommandEvent &event);
    void onSensorData(float value);
    std::vector<std::unique_ptr<TCPClient>> m_clients;


    // logging for debug
    std::ofstream m_logFile;
    void log(const wxString& message);
};
