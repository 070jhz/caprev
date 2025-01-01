#pragma once
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
};
