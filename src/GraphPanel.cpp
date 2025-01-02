#include "GraphPanel.h"
#include <wx/msw/colour.h>

GraphPanel::GraphPanel(wxWindow* parent) : wxPanel(parent)
{
    SetBackgroundColour(*wxWHITE);
    Bind(wxEVT_PAINT, &GraphPanel::OnPaint, this);
}

void GraphPanel::addPoint(float value) {
    m_points.push_back(value);
    if (m_points.size() > MAX_POINTS) {
        m_points.pop_front();
    }
    Refresh(); // trigger repaint
}

void GraphPanel::clear() {
    m_points.clear();
    Refresh();
}

void GraphPanel::onPaint(wxPaintEvent &event) {
    wxPaintDC dc(this);
    drawAxes(dc);
    drawData(dc);
}

void GraphPanel::drawAxes(wxDC& dc) {
    wxSize size = GetSize();
    dc.SetPen(wxPen(wxColour(0,0,0), 1));

    dc.DrawLine(10, size.GetHeight() - 10, size.GetWidth() - 10, size.GetHeight() - 10);
    dc.DrawLine(10, size.GetHeight() - 10, 10, 10);
}

void GraphPanel::drawData(wxDC& dc) {
    if (m_points.empty()) return;

    wxSize size = GetSize();
    dc.SetPen(wxPen(wxColour(0, 0, 255), 2));
    
    for (size_t i=1; i < m_points.size(); ++i) {
        wxPoint p1 = scalePoint(i-1, m_points[i-1]);
        wxPoint p2 = scalePoint(i, m_points[i]);
        dc.DrawLine(p1, p2);
    }
}

wxPoint GraphPanel::scalePoint(size_t x, float y) {
    wxSize size = GetSize();
    int scaledX = 10 + (size.GetWidth() - 20) * x / MAX_POINTS;
    int scaledY = size.GetHeight() - 10 - (size.GetHeight() - 20) * (y - m_minValue) / (m_maxValue - m_minValue);
    return wxPoint(scaledX,scaledY);
}
