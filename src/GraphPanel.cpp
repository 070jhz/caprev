#include "GraphPanel.h"
#include <windows.h>
#include <wx/datetime.h>
#include <wx/event.h>
#include <wx/font.h>
#include <wx/gdicmn.h>
#include <wx/msw/colour.h>
#include <wx/timer.h>
#include <wx/wx.h>
#include <wx/dcbuffer.h>

GraphPanel::GraphPanel(wxWindow* parent) :
    wxPanel(parent, wxID_ANY, wxDefaultPosition, wxDefaultSize, wxFULL_REPAINT_ON_RESIZE),
    m_minValue(0.0f),
    m_maxValue(100.0f),
    m_mousePos(wxDefaultPosition),
    m_showToolTip(false),
    m_viewOffset(0),
    m_isDragging(false),
    m_autoScrolling(true) {
    
    m_logFile.open("debuggp.log", std::ios::app);
    SetBackgroundStyle(wxBG_STYLE_PAINT);
    SetBackgroundColour(*wxWHITE);

    Bind(wxEVT_PAINT, &GraphPanel::onPaint, this);
    Bind(wxEVT_MOTION, &GraphPanel::onMouseMove, this);
    Bind(wxEVT_LEFT_DOWN, &GraphPanel::onMouseLeftDown, this);
    Bind(wxEVT_LEFT_UP, &GraphPanel::onMouseLeftUp, this);
    Bind(wxEVT_LEAVE_WINDOW, &GraphPanel::onMouseLeave, this);
    SetDoubleBuffered(true); // prevents flicker
}

void GraphPanel::addPoint(float value) {
    DataPoint point{value, wxDateTime::Now()};
    
    if (m_points.empty()) {
        m_firstPointTime = point.timestamp;
        m_startTime = point.timestamp;
    }
    m_points.push_back(point);
    
    // auto scroll
    if (m_autoScrolling && m_points.size() > MAX_POINTS) {
        m_viewOffset = m_points.size() - MAX_POINTS;
    }

    Refresh(); // trigger repaint
}

void GraphPanel::clear() {
    m_points.clear();
    m_viewOffset = 0;
    Refresh();
}

void GraphPanel::onPaint(wxPaintEvent &event) {
    wxBufferedPaintDC dc(this);
    dc.Clear();
    drawAxes(dc);
    drawData(dc);
    if (m_showToolTip) {
        drawToolTip(dc);
        drawMarker(dc);
    }
}

void GraphPanel::onMouseMove(wxMouseEvent &event) {
    m_mousePos = event.GetPosition();

    if (m_isDragging) {
        int dx = event.GetPosition().x - m_dragStart.x;
        if (abs(dx)>10) {
            m_viewOffset += (dx > 0) ? -1 : 1;
            m_viewOffset = std::max(0, m_viewOffset);
            
            m_autoScrolling = m_points.size() - 1 >= m_viewOffset && m_points.size()-1 < m_viewOffset + MAX_POINTS;
            m_dragStart = event.GetPosition();
            Refresh();
        }
    }
    else {
        wxPoint nearestPoint;
        m_showToolTip = isInPlotArea(m_mousePos) && isNearPoint(m_mousePos, nearestPoint);
        Refresh();
    }
}

void GraphPanel::log(const wxString& message) {
    if (m_logFile.is_open()) {
        m_logFile << wxDateTime::Now().Format("%Y-%m-%d %H:%M:%S: ").ToStdString()
                << message.ToStdString() << std::endl;
        m_logFile.flush();
    }
}

void GraphPanel::onMouseLeftDown(wxMouseEvent &event) {
    if (isInPlotArea(event.GetPosition())) {
        m_isDragging = true;
        m_dragStart = event.GetPosition();
        CaptureMouse();
    }
}

void GraphPanel::onMouseLeftUp(wxMouseEvent &event) {
    if (m_isDragging) {
        m_isDragging = false;
        if (HasCapture()) {
            ReleaseMouse();
        }
    }
}

void GraphPanel::onMouseLeave(wxMouseEvent &event) {
    onMouseLeftUp(event); // subject to change but works for the moment
    m_showToolTip = false;
    Refresh();
}

void GraphPanel::drawToolTip(wxDC& dc) {
    if (m_points.empty()) return;

    size_t idx = screenToIndex(m_mousePos.x);
    if (idx >= m_points.size()) return;

    auto& point = m_points[idx];
    wxString tooltip = wxString::Format("Value: %.2f\nTime: %s", point.value, point.timestamp.Format("%H:%M:%S"));

    wxString line1 = wxString::Format("Value: %.2f", point.value);
    wxString line2 = wxString::Format("Time: %s", point.timestamp.Format("%H:%M:%S"));

    wxSize line1Size = dc.GetTextExtent(line1);
    wxSize line2Size = dc.GetTextExtent(line2);

    int width = wxMax(line1Size.GetWidth(), line2Size.GetWidth());
    int height = line1Size.GetHeight() + line2Size.GetHeight();

    wxRect tooltipRect(m_mousePos.x + TOOLTIP_MARGIN,
                      m_mousePos.y + height - TOOLTIP_MARGIN,
                      width + 2*TOOLTIP_PADDING,
                      height + 2*TOOLTIP_PADDING);

    dc.SetBrush(*wxWHITE_BRUSH);
    dc.SetPen(*wxBLACK_PEN);
    dc.DrawRectangle(tooltipRect);
    dc.DrawText(tooltip, tooltipRect.GetX() + TOOLTIP_PADDING, tooltipRect.GetY() + TOOLTIP_PADDING);
}

void GraphPanel::drawMarker(wxDC& dc) {
    if (m_points.empty()) return;

    size_t idx = screenToIndex(m_mousePos.x);
    if (idx >= m_points.size()) return;

    wxPoint p = scalePoint(idx, m_points[idx].value);

    dc.SetPen(wxPen(*wxRED, 2));
    dc.SetBrush(*wxRED_BRUSH);
    dc.DrawCircle(p, MARKER_RADIUS);
}

void GraphPanel::drawAxes(wxDC& dc) {
    wxSize size = GetSize();
    dc.SetPen(wxPen(*wxBLACK, 2));
    
    dc.DrawLine(MARGIN_LEFT, size.GetHeight() - MARGIN_BOTTOM, size.GetWidth() - MARGIN_RIGHT, size.GetHeight() - MARGIN_BOTTOM);
    dc.DrawLine(MARGIN_LEFT, MARGIN_TOP, MARGIN_LEFT, size.GetHeight()-MARGIN_BOTTOM);

    drawAxisLabels(dc);
    drawTimeScale(dc);
    drawValueScale(dc);
}

void GraphPanel::drawAxisLabels(wxDC& dc) {
    wxSize size = GetSize();
    dc.SetFont(wxFont(FONT_SIZE, wxFONTFAMILY_DEFAULT, wxFONTSTYLE_NORMAL, wxFONTWEIGHT_NORMAL));

    dc.DrawText("Value", 5, size.GetHeight()/2);
    dc.DrawText("Time (s)", size.GetWidth() / 2, size.GetHeight() - MARGIN_BOTTOM/2);
}

void GraphPanel::drawTimeScale(wxDC& dc) {
    wxSize size = GetSize();
    int yPos = size.GetHeight() - MARGIN_BOTTOM;

    for(int i=0; i<=TICK_COUNT; i++) {
        int xPos = MARGIN_LEFT + (size.GetWidth() - MARGIN_LEFT - MARGIN_RIGHT) * i / TICK_COUNT;
        dc.DrawLine(xPos, yPos, xPos, yPos + TICK_LENGTH);

        // calculate index point for this tick
        size_t pointIndex = m_viewOffset + (i * MAX_POINTS / TICK_COUNT);
        if (pointIndex < m_points.size()) {
            wxTimeSpan diff = m_points[pointIndex].timestamp - m_firstPointTime;
            int seconds = diff.GetSeconds().ToLong();
            dc.DrawText(wxString::Format("%d", seconds), xPos - 5, yPos + TICK_LENGTH);
        }
    }
}

void GraphPanel::drawValueScale(wxDC& dc) {
    wxSize size = GetSize();
    for(int i = 0; i <= TICK_COUNT; i++) {
        int yPos = MARGIN_TOP + (size.GetHeight() - MARGIN_TOP - MARGIN_BOTTOM) * i / TICK_COUNT;
        dc.DrawLine(MARGIN_LEFT - TICK_LENGTH, yPos, MARGIN_LEFT, yPos);
        float value = m_maxValue - (m_maxValue - m_minValue) * i / TICK_COUNT;
        dc.DrawText(wxString::Format("%.1f", value), 5, yPos - 5);
    }
}

void GraphPanel::drawData(wxDC& dc) {
    if (m_points.empty()) return;

    dc.SetPen(wxPen(wxColour(0, 0, 255), DATA_LINE_THICKNESS));
    
    size_t startIdx = m_viewOffset;
    size_t endIdx = std::min(m_viewOffset + MAX_POINTS, m_points.size());

    for (size_t i=startIdx + 1; i < endIdx; ++i) {
        wxPoint p1 = scalePoint(i-1, m_points[i-1].value);
        wxPoint p2 = scalePoint(i, m_points[i].value);
        dc.DrawLine(p1, p2);
    }
}

wxPoint GraphPanel::scalePoint(size_t index, float y) {
    wxSize size = GetSize();

    // calculate available space
    int plotWidth = size.GetWidth() - (MARGIN_LEFT + MARGIN_RIGHT);
    int plotHeight = size.GetHeight() - (MARGIN_TOP + MARGIN_BOTTOM);

    // adjust x pos based on viewing window
    size_t displayIndex = index - m_viewOffset;
    int scaledX = MARGIN_LEFT + plotWidth * displayIndex / MAX_POINTS;
    int scaledY = size.GetHeight() - MARGIN_BOTTOM - ((y - m_minValue) * plotHeight) / (m_maxValue - m_minValue);
    return wxPoint(scaledX,scaledY);
}

bool GraphPanel::isInPlotArea(const wxPoint& pos) {
    return pos.x >= MARGIN_LEFT &&
           pos.x <= GetSize().GetWidth() - MARGIN_RIGHT &&
           pos.y >= MARGIN_TOP &&
           pos.y <= GetSize().GetHeight() - MARGIN_BOTTOM;
}

bool GraphPanel::isNearPoint(const wxPoint &mousePos, wxPoint &nearestPoint) {
    if (m_points.empty()) return false;

    size_t idx = screenToIndex(mousePos.x);
    if (idx >= m_points.size()) return false;

    nearestPoint = scalePoint(idx, m_points[idx].value);

    int dx = mousePos.x - nearestPoint.x;
    int dy = mousePos.y - nearestPoint.y;
    int distanceSquared = dx*dx + dy*dy;

    return distanceSquared <= PROXIMITY_THRESHOLD * PROXIMITY_THRESHOLD;
}

size_t GraphPanel::screenToIndex(int x) {
    wxSize size = GetSize();

    // available width for plotting (excluding margins)
    int plotWidth = size.GetWidth() - (MARGIN_LEFT + MARGIN_RIGHT);

    // convert screen X to index (inverse of scalePoint X calculation)
    size_t displayIndex = ((x - MARGIN_LEFT) * MAX_POINTS) / plotWidth;

    return m_viewOffset + displayIndex;
}
