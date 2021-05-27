﻿// Enums.cs
//
// Copyright (C) BEditor
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.

namespace BEditor.Compute.OpenCL
{
#pragma warning disable CS1591, SA1600, SA1602
    public enum CLDeviceType : long
    {
        CL_DEVICE_TYPE_DEFAULT = 1 << 0,
        CL_DEVICE_TYPE_CPU = 1 << 1,
        CL_DEVICE_TYPE_GPU = 1 << 2,
        CL_DEVICE_TYPE_ACCELERATOR = 1 << 3,
        CL_DEVICE_TYPE_ALL = 0xFFFFFFFF,
    }

    public enum CLPlatformInfo : long
    {
        CL_PLATFORM_PROFILE = 0x0900,
        CL_PLATFORM_VERSION = 0x0901,
        CL_PLATFORM_NAME = 0x0902,
        CL_PLATFORM_VENDOR = 0x0903,
        CL_PLATFORM_EXTENSIONS = 0x0904,
    }

    public enum CLDeviceInfo : long
    {
        CL_DEVICE_TYPE = 0x1000,
        CL_DEVICE_VENDOR_ID = 0x1001,
        CL_DEVICE_MAX_COMPUTE_UNITS = 0x1002,
        CL_DEVICE_MAX_WORK_ITEM_DIMENSIONS = 0x1003,
        CL_DEVICE_MAX_WORK_GROUP_SIZE = 0x1004,
        CL_DEVICE_MAX_WORK_ITEM_SIZES = 0x1005,
        CL_DEVICE_PREFERRED_VECTOR_WIDTH_CHAR = 0x1006,
        CL_DEVICE_PREFERRED_VECTOR_WIDTH_SHORT = 0x1007,
        CL_DEVICE_PREFERRED_VECTOR_WIDTH_INT = 0x1008,
        CL_DEVICE_PREFERRED_VECTOR_WIDTH_LONG = 0x1009,
        CL_DEVICE_PREFERRED_VECTOR_WIDTH_FLOAT = 0x100A,
        CL_DEVICE_PREFERRED_VECTOR_WIDTH_DOUBLE = 0x100B,
        CL_DEVICE_MAX_CLOCK_FREQUENCY = 0x100C,
        CL_DEVICE_ADDRESS_BITS = 0x100D,
        CL_DEVICE_MAX_READ_IMAGE_ARGS = 0x100E,
        CL_DEVICE_MAX_WRITE_IMAGE_ARGS = 0x100F,
        CL_DEVICE_MAX_MEM_ALLOC_SIZE = 0x1010,
        CL_DEVICE_IMAGE2D_MAX_WIDTH = 0x1011,
        CL_DEVICE_IMAGE2D_MAX_HEIGHT = 0x1012,
        CL_DEVICE_IMAGE3D_MAX_WIDTH = 0x1013,
        CL_DEVICE_IMAGE3D_MAX_HEIGHT = 0x1014,
        CL_DEVICE_IMAGE3D_MAX_DEPTH = 0x1015,
        CL_DEVICE_IMAGE_SUPPORT = 0x1016,
        CL_DEVICE_MAX_PARAMETER_SIZE = 0x1017,
        CL_DEVICE_MAX_SAMPLERS = 0x1018,
        CL_DEVICE_MEM_BASE_ADDR_ALIGN = 0x1019,
        CL_DEVICE_MIN_DATA_TYPE_ALIGN_SIZE = 0x101A,
        CL_DEVICE_SINGLE_FP_CONFIG = 0x101B,
        CL_DEVICE_GLOBAL_MEM_CACHE_TYPE = 0x101C,
        CL_DEVICE_GLOBAL_MEM_CACHELINE_SIZE = 0x101D,
        CL_DEVICE_GLOBAL_MEM_CACHE_SIZE = 0x101E,
        CL_DEVICE_GLOBAL_MEM_SIZE = 0x101F,
        CL_DEVICE_MAX_CONSTANT_BUFFER_SIZE = 0x1020,
        CL_DEVICE_MAX_CONSTANT_ARGS = 0x1021,
        CL_DEVICE_LOCAL_MEM_TYPE = 0x1022,
        CL_DEVICE_LOCAL_MEM_SIZE = 0x1023,
        CL_DEVICE_ERROR_CORRECTION_SUPPORT = 0x1024,
        CL_DEVICE_PROFILING_TIMER_RESOLUTION = 0x1025,
        CL_DEVICE_ENDIAN_LITTLE = 0x1026,
        CL_DEVICE_AVAILABLE = 0x1027,
        CL_DEVICE_COMPILER_AVAILABLE = 0x1028,
        CL_DEVICE_EXECUTION_CAPABILITIES = 0x1029,
        CL_DEVICE_QUEUE_PROPERTIES = 0x102A,
        CL_DEVICE_NAME = 0x102B,
        CL_DEVICE_VENDOR = 0x102C,
        CL_DRIVER_VERSION = 0x102D,
        CL_DEVICE_PROFILE = 0x102E,
        CL_DEVICE_VERSION = 0x102F,
        CL_DEVICE_EXTENSIONS = 0x1030,
        CL_DEVICE_PLATFORM = 0x1031,
        CL_DEVICE_DOUBLE_FP_CONFIG = 0x1032,
        CL_DEVICE_PREFERRED_VECTOR_WIDTH_HALF = 0x1034,
        CL_DEVICE_HOST_UNIFIED_MEMORY = 0x1035,
        CL_DEVICE_NATIVE_VECTOR_WIDTH_CHAR = 0x1036,
        CL_DEVICE_NATIVE_VECTOR_WIDTH_SHORT = 0x1037,
        CL_DEVICE_NATIVE_VECTOR_WIDTH_INT = 0x1038,
        CL_DEVICE_NATIVE_VECTOR_WIDTH_LONG = 0x1039,
        CL_DEVICE_NATIVE_VECTOR_WIDTH_FLOAT = 0x103A,
        CL_DEVICE_NATIVE_VECTOR_WIDTH_DOUBLE = 0x103B,
        CL_DEVICE_NATIVE_VECTOR_WIDTH_HALF = 0x103C,
        CL_DEVICE_OPENCL_C_VERSION = 0x103D,
        CL_DEVICE_LINKER_AVAILABLE = 0x103E,
        CL_DEVICE_BUILT_IN_KERNELS = 0x103F,
        CL_DEVICE_IMAGE_MAX_BUFFER_SIZE = 0x1040,
        CL_DEVICE_IMAGE_MAX_ARRAY_SIZE = 0x1041,
        CL_DEVICE_PARENT_DEVICE = 0x1042,
        CL_DEVICE_PARTITION_MAX_SUB_DEVICES = 0x1043,
        CL_DEVICE_PARTITION_PROPERTIES = 0x1044,
        CL_DEVICE_PARTITION_AFFINITY_DOMAIN = 0x1045,
        CL_DEVICE_PARTITION_TYPE = 0x1046,
        CL_DEVICE_REFERENCE_COUNT = 0x1047,
        CL_DEVICE_PREFERRED_INTEROP_USER_SYNC = 0x1048,
        CL_DEVICE_IMAGE_PITCH_ALIGNMENT = 0x104A,
        CL_DEVICE_IMAGE_BASE_ADDRESS_ALIGNMENT = 0x104B,
        CL_DEVICE_MAX_READ_WRITE_IMAGE_ARGS = 0x104C,
        CL_DEVICE_MAX_GLOBAL_VARIABLE_SIZE = 0x104D,
        CL_DEVICE_QUEUE_ON_DEVICE_PROPERTIES = 0x104E,
        CL_DEVICE_QUEUE_ON_DEVICE_PREFERRED_SIZE = 0x104F,
        CL_DEVICE_QUEUE_ON_DEVICE_MAX_SIZE = 0x1050,
        CL_DEVICE_MAX_ON_DEVICE_QUEUES = 0x1051,
        CL_DEVICE_MAX_ON_DEVICE_EVENTS = 0x1052,
        CL_DEVICE_SVM_CAPABILITIES = 0x1053,
        CL_DEVICE_GLOBAL_VARIABLE_PREFERRED_TOTAL_SIZE = 0x1054,
        CL_DEVICE_MAX_PIPE_ARGS = 0x1055,
        CL_DEVICE_PIPE_MAX_ACTIVE_RESERVATIONS = 0x1056,
        CL_DEVICE_PIPE_MAX_PACKET_SIZE = 0x1057,
        CL_DEVICE_PREFERRED_PLATFORM_ATOMIC_ALIGNMENT = 0x1058,
        CL_DEVICE_PREFERRED_GLOBAL_ATOMIC_ALIGNMENT = 0x1059,
        CL_DEVICE_PREFERRED_LOCAL_ATOMIC_ALIGNMENT = 0x105A,
        CL_DEVICE_IL_VERSION = 0x105B,
        CL_DEVICE_MAX_NUM_SUB_GROUPS = 0x105C,
        CL_DEVICE_SUB_GROUP_INDEPENDENT_FORWARD_PROGRESS = 0x105D,
    }

    public enum CLDeviceFpConfig : long
    {
        CL_FP_DENORM = 1 << 0,
        CL_FP_INF_NAN = 1 << 1,
        CL_FP_ROUND_TO_NEAREST = 1 << 2,
        CL_FP_ROUND_TO_ZERO = 1 << 3,
        CL_FP_ROUND_TO_INF = 1 << 4,
        CL_FP_FMA = 1 << 5,
        CL_FP_SOFT_FLOAT = 1 << 6,
        CL_FP_CORRECTLY_ROUNDED_DIVIDE_SQRT = 1 << 7,
    }

    public enum CLDeviceMemoryCacheType : long
    {
        CL_NONE = 0x0,
        CL_READ_ONLY_CACHE = 0x1,
        CL_READ_WRITE_CACHE = 0x2,
    }

    public enum CLDeviceLocalMemoryType : long
    {
        CL_LOCAL = 0x1,
        CL_GLOBAL = 0x2,
    }

    public enum CLDeviceExecCapabilities : long
    {
        CL_EXEC_KERNEL = 1 << 0,
        CL_EXEC_NATIVE_KERNEL = 1 << 1,
    }

    public enum CLCommandQueueProperties : long
    {
        CL_QUEUE_OUT_OF_ORDER_EXEC_MODE_ENABLE = 1 << 0,
        CL_QUEUE_PROFILING_ENABLE = 1 << 1,
        CL_QUEUE_ON_DEVICE = 1 << 2,
        CL_QUEUE_ON_DEVICE_DEFAULT = 1 << 3,
    }

    public enum CLMemoryFlags : long
    {
        CL_MEM_READ_WRITE = 1 << 0,
        CL_MEM_WRITE_ONLY = 1 << 1,
        CL_MEM_READ_ONLY = 1 << 2,
        CL_MEM_USE_HOST_PTR = 1 << 3,
        CL_MEM_ALLOC_HOST_PTR = 1 << 4,
        CL_MEM_COPY_HOST_PTR = 1 << 5,
    }

    public enum CLMapFlags : long
    {
        CL_MAP_READ = 1 << 0,
        CL_MAP_WRITE = 1 << 1,
        CL_MAP_WRITE_INVALIDATE_REGION = 1 << 2,
    }

    public enum CLDeviceSVMCapabilities : long
    {
        CL_DEVICE_SVM_COARSE_GRAIN_BUFFER = 1 << 0,
        CL_DEVICE_SVM_FINE_GRAIN_BUFFER = 1 << 1,
        CL_DEVICE_SVM_FINE_GRAIN_SYSTEM = 1 << 2,
        CL_DEVICE_SVM_ATOMICS = 1 << 3,
    }

    public enum CLProgramBuildInfo : long
    {
        CL_PROGRAM_BUILD_STATUS = 0x1181,
        CL_PROGRAM_BUILD_OPTIONS = 0x1182,
        CL_PROGRAM_BUILD_LOG = 0x1183,
        CL_PROGRAM_BINARY_TYPE = 0x1184,
        CL_PROGRAM_BUILD_GLOBAL_VARIABLE_TOTAL_SIZE = 0x1185,
    }

    public enum CLProfilingInfo
    {
        CL_PROFILING_COMMAND_QUEUED = 0x1280,
        CL_PROFILING_COMMAND_SUBMIT = 0x1281,
        CL_PROFILING_COMMAND_START = 0x1282,
        CL_PROFILING_COMMAND_END = 0x1283,
        CL_PROFILING_COMMAND_COMPLETE = 0x1284,
    }

    public enum CLStatusCode
    {
        CL_INVALID_DEVICE_QUEUE = -70,
        CL_INVALID_PIPE_SIZE = -69,
        CL_INVALID_DEVICE_PARTITION_COUNT = -68,
        CL_INVALID_LINKER_OPTIONS = -67,
        CL_INVALID_COMPILER_OPTIONS = -66,
        CL_INVALID_IMAGE_DESCRIPTOR = -65,
        CL_INVALID_PROPERTY = -64,
        CL_INVALID_GLOBAL_WORK_SIZE = -63,
        CL_INVALID_MIP_LEVEL = -62,
        CL_INVALID_BUFFER_SIZE = -61,
        CL_INVALID_GL_OBJECT = -60,
        CL_INVALID_OPERATION = -59,
        CL_INVALID_EVENT = -58,
        CL_INVALID_EVENT_WAIT_LIST = -57,
        CL_INVALID_GLOBAL_OFFSET = -56,
        CL_INVALID_WORK_ITEM_SIZE = -55,
        CL_INVALID_WORK_GROUP_SIZE = -54,
        CL_INVALID_WORK_DIMENSION = -53,
        CL_INVALID_KERNEL_ARGS = -52,
        CL_INVALID_ARG_SIZE = -51,
        CL_INVALID_ARG_VALUE = -50,
        CL_INVALID_ARG_INDEX = -49,
        CL_INVALID_KERNEL = -48,
        CL_INVALID_KERNEL_DEFINITION = -47,
        CL_INVALID_KERNEL_NAME = -46,
        CL_INVALID_PROGRAM_EXECUTABLE = -45,
        CL_INVALID_PROGRAM = -44,
        CL_INVALID_BUILD_OPTIONS = -43,
        CL_INVALID_BINARY = -42,
        CL_INVALID_SAMPLER = -41,
        CL_INVALID_IMAGE_SIZE = -40,
        CL_INVALID_IMAGE_FORMAT_DESCRIPTOR = -39,
        CL_INVALID_MEM_OBJECT = -38,
        CL_INVALID_HOST_PTR = -37,
        CL_INVALID_COMMAND_QUEUE = -36,
        CL_INVALID_QUEUE_PROPERTIES = -35,
        CL_INVALID_CONTEXT = -34,
        CL_INVALID_DEVICE = -33,
        CL_INVALID_PLATFORM = -32,
        CL_INVALID_DEVICE_TYPE = -31,
        CL_INVALID_VALUE = -30,
        CL_KERNEL_ARG_INFO_NOT_AVAILABLE = -19,
        CL_DEVICE_PARTITION_FAILED = -18,
        CL_LINK_PROGRAM_FAILURE = -17,
        CL_LINKER_NOT_AVAILABLE = -16,
        CL_COMPILE_PROGRAM_FAILURE = -15,
        CL_EXEC_STATUS_ERROR_FOR_EVENTS_IN_WAIT_LIST = -14,
        CL_MISALIGNED_SUB_BUFFER_OFFSET = -13,
        CL_MAP_FAILURE = -12,
        CL_BUILD_PROGRAM_FAILURE = -11,
        CL_IMAGE_FORMAT_NOT_SUPPORTED = -10,
        CL_IMAGE_FORMAT_MISMATCH = -9,
        CL_MEM_COPY_OVERLAP = -8,
        CL_PROFILING_INFO_NOT_AVAILABLE = -7,
        CL_OUT_OF_HOST_MEMORY = -6,
        CL_OUT_OF_RESOURCES = -5,
        CL_MEM_OBJECT_ALLOCATION_FAILURE = -4,
        CL_COMPILER_NOT_AVAILABLE = -3,
        CL_DEVICE_NOT_AVAILABLE = -2,
        CL_DEVICE_NOT_FOUND = -1,
        CL_SUCCESS = 0,
    }
#pragma warning restore SA1600, CS1591, SA1602
}