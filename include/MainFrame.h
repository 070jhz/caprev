
#include <wx/event.h>
#include <wx/wx.h>

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
    
    DECLARE_EVENT_TABLE()
};
