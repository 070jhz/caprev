#pragma once
#include <wx/event.h>
#include <wx/wx.h>
#include <deque>
#include <fstream>

class GraphPanel : public wxPanel {
public:
    explicit GraphPanel(wxWindow *parent);

    void resetTime() { m_startTime = wxDateTime::Now(); }
    void addPoint(float value);
    void clear();

private:
    struct DataPoint {
        float value;
        wxDateTime timestamp;
    };

    void onPaint(wxPaintEvent &event);
    void onMouseMove(wxMouseEvent &event);
    void onMouseLeftDown(wxMouseEvent &event);
    void onMouseLeftUp(wxMouseEvent &event);
    void onMouseLeave(wxMouseEvent &event);
    void drawTimeScale(wxDC& dc);
    void drawValueScale(wxDC& dc);
    void drawAxes(wxDC& dc);
    void drawData(wxDC& dc);
    void drawAxisLabels(wxDC& dc);
    void drawToolTip(wxDC& dc);
    void drawMarker(wxDC& dc);
    wxPoint scalePoint(size_t x, float y);
    size_t screenToIndex(int x);
    bool isInPlotArea(const wxPoint &pos);
    bool isNearPoint(const wxPoint &mousePos, wxPoint &nearestPoint);

    std::deque<DataPoint> m_points;
    float m_minValue;
    float m_maxValue;
    wxDateTime m_startTime;
    wxDateTime m_firstPointTime;
    wxPoint m_mousePos;
    wxPoint m_dragStart;
    int m_viewOffset;
    bool m_isDragging;
    bool m_autoScrolling;
    bool m_showToolTip;
    
    // logging for debug
    std::ofstream m_logFile;
    void log(const wxString& message);

    static constexpr size_t MAX_POINTS = 10;
    static constexpr int MARGIN_LEFT = 40;
    static constexpr int MARGIN_RIGHT = 10;
    static constexpr int MARGIN_TOP = 10;
    static constexpr int MARGIN_BOTTOM = 30;
    static constexpr int AXIS_THICKNESS = 2;
    static constexpr int DATA_LINE_THICKNESS = 2;
    static constexpr int FONT_SIZE = 8;
    static constexpr int TICK_LENGTH = 5; 
    static constexpr int TICK_COUNT = 10;
    static constexpr int TOOLTIP_PADDING = 5;
    static constexpr int TOOLTIP_MARGIN = 10;
    static constexpr int MARKER_RADIUS = 4;
    static constexpr int PROXIMITY_THRESHOLD = 20;
};
