#include <wx/event.h>
#include <wx/wx.h>
#include <wx/tglbtn.h>
#include <memory>
#include <vector>
#include <fstream>
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
    void onSensorSelected(wxCommandEvent &event);
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

    // logging for debug
    std::ofstream m_logFile;
    void log(const wxString& message);
};
