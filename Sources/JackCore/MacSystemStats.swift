import Foundation
import IOKit.ps

extension JackRemote.SystemStats {
    /// Lo que Jack lee ahora de esta Mac. Solo macOS (usa IOKit): el cliente iOS solo decodifica la estructura.
    public static func current() -> JackRemote.SystemStats {
        var battery: Double?
        var charging = false
        let info = IOPSCopyPowerSourcesInfo().takeRetainedValue()
        for source in IOPSCopyPowerSourcesList(info).takeRetainedValue() as [CFTypeRef] {
            guard let d = IOPSGetPowerSourceDescription(info, source)?.takeUnretainedValue() as? [String: Any],
                  d[kIOPSTypeKey] as? String == kIOPSInternalBatteryType,
                  let now = d[kIOPSCurrentCapacityKey] as? Double, let max = d[kIOPSMaxCapacityKey] as? Double, max > 0
            else { continue }
            battery = now / max * 100
            charging = d[kIOPSIsChargingKey] as? Bool ?? false
        }
        let total = Double(ProcessInfo.processInfo.physicalMemory)
        var stats = vm_statistics64()
        var count = mach_msg_type_number_t(MemoryLayout<vm_statistics64>.size / MemoryLayout<integer_t>.size)
        let ok = withUnsafeMutablePointer(to: &stats) {
            $0.withMemoryRebound(to: integer_t.self, capacity: Int(count)) { host_statistics64(mach_host_self(), HOST_VM_INFO64, $0, &count) }
        } == KERN_SUCCESS
        // Como el Monitor de Actividad: memoria usada = activa + cableada + comprimida.
        let used = ok ? Double(UInt64(stats.active_count) + UInt64(stats.wire_count) + UInt64(stats.compressor_page_count)) * Double(vm_kernel_page_size) : 0
        let disk = try? URL(fileURLWithPath: "/").resourceValues(forKeys: [.volumeAvailableCapacityForImportantUsageKey, .volumeTotalCapacityKey])
        return JackRemote.SystemStats(batteryPercent: battery, batteryCharging: charging, memoryUsedBytes: used, memoryTotalBytes: total,
                                      diskAvailableBytes: Double(disk?.volumeAvailableCapacityForImportantUsage ?? 0),
                                      diskTotalBytes: Double(disk?.volumeTotalCapacity ?? 0))
    }
}
