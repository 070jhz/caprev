#include "MainFrame.h"
#include <wx/event.h>
#include <wx/gdicmn.h>
#include <wx/msw/button.h>
#include <wx/textctrl.h>

MainFrame::MainFrame() :
    wxFrame(nullptr, wxID_ANY, "Sensor Monitor", wxDefaultPosition, wxSize(800,600)) {
    
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

    leftSizer->Add(new wxStaticText(m_leftPanel, wxID_ANY, "Enter Sensor PIN:"),
                   0, wxALL, 5);
    leftSizer->Add(m_pinInput, 0, wxEXPAND | wxALL, 5);
    leftSizer->Add(m_connectBtn, 0, wxEXPAND | wxALL, 5);
    leftSizer->Add(new wxStaticText(m_leftPanel, wxID_ANY, "Connected Sensors:"),
                   0, wxTOP | wxLEFT, 10);
    leftSizer->Add(m_sensorList, 1, wxEXPAND | wxALL, 5);

    m_leftPanel->SetSizer(leftSizer);

    // add panel to main layout
    mainSizer->Add(m_leftPanel, 3, wxEXPAND | wxALL, 5);
    mainSizer->Add(m_rightPanel, 7, wxEXPAND | wxALL);
    SetSizer(mainSizer);

    m_connectBtn->Bind(wxEVT_BUTTON, &MainFrame::onConnect, this);
    m_pinInput->Bind(wxEVT_TEXT_ENTER, &MainFrame::onConnect,this);
}

void MainFrame::onExit(wxCommandEvent &event) { Close(true); }

void MainFrame::onAbout(wxCommandEvent &event) {
    wxMessageBox("Caprev Companion App \nVR Sensor Monitor", "About Caprev",
                 wxOK | wxICON_INFORMATION);
}

void MainFrame::onConnect(wxCommandEvent &event) {
    wxString pin = m_pinInput->GetValue().Trim();

    if (pin.IsEmpty()) {
        wxMessageBox("Please enter a PIN", "Error", wxOK | wxICON_ERROR);
        return;
    }

    // add to history if not already present
    if (m_sensorList->FindString(pin) == wxNOT_FOUND) {
        m_sensorList->Append(pin);
    }

    m_pinInput->Clear();
    SetStatusText("Connecting to sensor " + pin + "...");
}



