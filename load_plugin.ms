(
    local scriptPath = getSourceFileName()
    local scriptDir = getFilenamePath scriptPath

    local dllPath = scriptDir + @"bin\Debug\EasyMaxWeightedNormal.dll"

    if doesFileExist dllPath then
    (
        dotNet.loadAssembly dllPath

        local asmFunc = dotNetClass "EasyMaxWeightedNormal.AssemblyFunctions"
        asmFunc.AssemblyMain()

        format "[EasyWeightedNormal] Loaded: %\n" dllPath
    )
    else
    (
        format "[EasyWeightedNormal] DLL not found: %\n" dllPath
    )
)