#include "idle.h"

#if defined(Q_OS_WIN)
#include <windows.h>
quint64 systemIdleMs() {
    LASTINPUTINFO lii;
    lii.cbSize = sizeof(lii);
    if (!GetLastInputInfo(&lii)) return 0;
    return (quint64)(GetTickCount() - lii.dwTime);
}
#elif defined(Q_OS_MACOS)
#include <ApplicationServices/ApplicationServices.h>
quint64 systemIdleMs() {
    double seconds = CGEventSourceSecondsSinceLastEventType(
        kCGEventSourceStateCombinedSessionState, kCGAnyInputEventType);
    return (quint64)(seconds * 1000.0);
}
#elif defined(Q_OS_LINUX)
// X11 reference implementation would use XScreenSaverQueryInfo (requires libXss).
quint64 systemIdleMs() { return 0; }
#else
quint64 systemIdleMs() { return 0; }
#endif
