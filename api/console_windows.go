//go:build windows

package main

import (
	"syscall"
	"unsafe"
)

func initConsole() {
	kernel32 := syscall.NewLazyDLL("kernel32.dll")
	setMode := kernel32.NewProc("SetConsoleMode")
	getMode := kernel32.NewProc("GetConsoleMode")
	for _, h := range []int{syscall.STD_OUTPUT_HANDLE, syscall.STD_ERROR_HANDLE} {
		handle, _ := syscall.GetStdHandle(h)
		var mode uint32
		getMode.Call(uintptr(handle), uintptr(unsafe.Pointer(&mode)))
		setMode.Call(uintptr(handle), uintptr(mode|0x0004))
	}
}
