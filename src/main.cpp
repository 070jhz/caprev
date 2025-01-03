#include "MainFrame.h"
#include <wx/wx.h>

class CaprevApp : public wxApp {
public:
    bool OnInit() override {
        if (!wxApp::OnInit()) {
            return false;
        }

        auto frame = new MainFrame();
        if (!frame) {
            return false;
        }

        frame->SetSize(800, 600);

        frame->Show(true);
        frame->Centre();

        return true;
    }
};

wxIMPLEMENT_APP(CaprevApp);
