package main

import "sync"

var vjSessionMeta struct {
	sync.RWMutex
	byID map[string]bool
}

func vjSessionMetaInit() {
	vjSessionMeta.byID = make(map[string]bool)
}

func getAudienceParticipation(sessionID string) bool {
	sid := normalizeVJSessionID(sessionID)
	vjSessionMeta.RLock()
	defer vjSessionMeta.RUnlock()
	return vjSessionMeta.byID[sid]
}

func setAudienceParticipation(sessionID string, enabled bool) {
	sid := normalizeVJSessionID(sessionID)
	vjSessionMeta.Lock()
	vjSessionMeta.byID[sid] = enabled
	vjSessionMeta.Unlock()
}
