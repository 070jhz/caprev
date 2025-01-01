#include "MainFrame.h"
#include <iostream>
#include <memory>
#include <wx/wx.h>

class CaprevApp : public wxApp {
public:
  bool OnInit() override {
    try {
      if (!wxApp::OnInit()) {
        std::cerr << "wxApp failed to init" << std::endl;
        return false;
      }

      std::cout << "creating main frame..." << std::endl;
      auto frame = new MainFrame();
      if (!frame) {
        std::cerr << "failed to create mainframe" << std::endl;
        return false;
      }

      std::cout << "Setting frame size..." << std::endl;
      frame->SetSize(800, 600);

      std::cout << "Showing frame..." << std::endl;
      frame->Show(true);
      frame->Centre();

      return true;
    } catch (const std::exception &e) {
      std::cerr << "fatal exception: " << e.what() << std::endl;
      return false;
    }
  }
};

wxIMPLEMENT_APP(CaprevApp);
