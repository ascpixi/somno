#pragma once

// Custom macros

// Defines a 32-bit ASCII allocation tag.
#define ALLOC_TAG(tag)					 \
	(									 \
		((tag << 24) & 0xFF000000) |	 \
		((tag << 8) & 0x00FF0000)  |	 \
		((tag >> 8) & 0x0000FF00)  |	 \
		((tag >> 24) & 0x000000FF)		 \
	)

// Undocumented structure/function definitions

#define SystemModuleInformation 0x0B

extern NTSTATUS NTAPI MmCopyVirtualMemory(
	PEPROCESS SourceProcess,
	PVOID SourceAddress,
	PEPROCESS TargetProcess,
	PVOID TargetAddress,
	SIZE_T BufferSize,
	KPROCESSOR_MODE PreviousMode,
	PSIZE_T ReturnSize
);

extern NTSTATUS NTAPI ZwQuerySystemInformation(
	ULONG InfoClass,
	PVOID Buffer,
	ULONG Length,
	PULONG ReturnLength
);

extern PVOID NTAPI RtlFindExportedRoutineByName(
	_In_ PVOID ImageBase,
	_In_ PCCH RoutineName
);

typedef struct _RTL_PROCESS_MODULE_INFORMATION {
	HANDLE Section;
	PVOID MappedBase;
	PVOID ImageBase;
	ULONG ImageSize;
	ULONG Flags;
	USHORT LoadOrderIndex;
	USHORT InitOrderIndex;
	USHORT LoadCount;
	USHORT OffsetToFileName;
	UCHAR FullPathName[256];
} RTL_PROCESS_MODULE_INFORMATION, * PRTL_PROCESS_MODULE_INFORMATION;

typedef struct _RTL_PROCESS_MODULES {
	ULONG NumberOfModules;
	RTL_PROCESS_MODULE_INFORMATION Modules[1];
} RTL_PROCESS_MODULES, * PRTL_PROCESS_MODULES;