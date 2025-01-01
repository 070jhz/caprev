#include <wx/event.h>
#include <wx/wx.h>
#include <memory>
#include <vector>
#include <fstream>
#include "Sensor.h"

// main window class

class MainFrame : public wxFrame {
public:
    MainFrame();
    virtual ~MainFrame() = default;

private:
    // event handlers
    void onConnect(wxCommandEvent &event);
    void onExit(wxCommandEvent &event);
    void onAbout(wxCommandEvent &event);

    // gui elements
    wxMenuBar *m_menuBar;
    wxStatusBar *m_statusBar;
    wxPanel *m_leftPanel; // sensor connection
    wxPanel *m_rightPanel; // sensor data
    wxTextCtrl *m_pinInput;
    wxButton* m_connectBtn;
    wxListBox* m_sensorList;

    // sensor management
    std::vector<std::unique_ptr<Sensor>> m_sensors;
    
    // debug logging system
    std::ofstream m_logFile;
    void log(const wxString& message);
};
