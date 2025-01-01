#pragma once
#include <wx/event.h>
#include <wx/wx.h>

// main window class

class MainFrame : public wxFrame {
public:
    MainFrame();

private:
    // event handlers
    void onExit(wxCommandEvent &event);
    void onAbout(wxCommandEvent &event);

    // gui elements
    wxMenuBar *m_menuBar;
    wxStatusBar *m_statusBar;
    wxPanel *leftPanel; // sensor list
    wxPanel *rightPanel; // sensor data
    wxButton* m_connectBtn;
    wxListBox* m_sensorList;
    
    DECLARE_EVENT_TABLE();
};
