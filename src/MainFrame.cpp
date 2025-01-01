#include "MainFrame.h"

MainFrame::MainFrame() : wxFrame(nullptr, wxID_ANY, "Caprev - System Monitor") {
  wxMenu *menuFile = new wxMenu;
  menuFile->Append(wxID_EXIT, "&Exit\tAlt-X", "Close the application");

  wxMenu *menuHelp = new wxMenu;
  menuHelp->Append(wxID_ABOUT, "&About\tF1", "Show about dialog");

  m_menuBar = new wxMenuBar;
  m_menuBar->Append(menuFile, "&File");
  m_menuBar->Append(menuHelp, "&Help");

  SetMenuBar(m_menuBar);

  m_statusBar = CreateStatusBar();
  SetStatusText("Welcome to Caprev !");

  Bind(wxEVT_MENU, &MainFrame::onExit, this, wxID_EXIT);
  Bind(wxEVT_MENU, &MainFrame::onAbout, this, wxID_ABOUT);
}

void MainFrame::onExit(wxCommandEvent &event) { Close(true); }

void MainFrame::onAbout(wxCommandEvent &event) {
  wxMessageBox("Caprev Companion App \nData monitoring tool", "About Caprev",
               wxOK | wxICON_INFORMATION);
}
