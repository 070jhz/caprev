#pragma once
#include <wx/wx.h>
#include <deque>

class GraphPanel : public wxPanel {
public:
    explicit GraphPanel(wxWindow *parent);

    void addPoint(float value);
    void clear();

private:
    void onPaint(wxPaintEvent &evt);
    void drawAxes(wxDC& dc);
    void drawData(wxDC& dc);
    wxPoint scalePoint(size_t x, float y);

    std::deque<float> m_points;
    float m_minValue;
    float m_maxValue;
    static constexpr size_t MAX_POINTS = 100;
};
