# Generates the main distribution archive.

if (!($args.Length -eq 1)) {
	write-host "Usage: mkmain.ps1 [ version ]`n"
	return;
}

$ver = $args[0]

if (test-path ../bin/dist/main) {
	rm ../bin/dist/main -recurse -force
}

write-host "Building distribution archive..." -ForegroundColor green

mkdir ../bin/dist/main | out-null

cp ./etc/* ../bin/dist/main/

mkdir ../bin/dist/main/bin | out-null

cp ../bin/Release/fuse.exe ../bin/dist/main/bin/
cp ../bin/Release/fuse.exe.config ../bin/dist/main/bin/
cp ../bin/Release/Fusion.tasks ../bin/dist/main/bin/
cp ../bin/Release/fusion-config.exe ../bin/dist/main/bin/
cp ../bin/Release/fusion-config.exe.config ../bin/dist/main/bin/
cp ../bin/Release/libfusion.dll ../bin/dist/main/bin/
cp ../bin/Release/libfusiontasks.dll ../bin/dist/main/bin/
cp ../bin/Release/sandbox.exe ../bin/dist/main/bin/
cp ../bin/Release/sandbox.exe.config ../bin/dist/main/bin/

cp ../lib/libconsole2.dll ../bin/dist/main/bin/
cp ../lib/log4net.dll ../bin/dist/main/bin/
cp ../lib/Nini.dll ../bin/dist/main/bin/
cp ../lib/System.Data.SQLite.dll ../bin/dist/main/bin/
cp ../lib/System.Data.SQLite.Linq.dll ../bin/dist/main/bin/

cp -R ./cygwin ../bin/dist/main/bin/

mkdir ../bin/dist/main/bin/ExtensionPack | out-null
cp ./ExtensionPack/* ../bin/dist/main/bin/ExtensionPack/
attrib -R ../bin/dist/main/bin/ExtensionPack/*

rm ../bin/dist/fusion-$ver.zip -ErrorAction SilentlyContinue
./7za.exe a -r -tzip ../bin/dist/fusion-$ver.zip ../bin/dist/main/*