# Generates the bootstrap archive.

if (!($args.Length -eq 1)) {
	write-host "Usage: mkbootstrap.ps1 [ port ]`n"
	return;
}

$port = $args[0]

if (test-path ../bin/dist/bootstrap) {
	rm ../bin/dist/bootstrap -recurse -force
}

write-host "Building bootstrap archive..." -ForegroundColor green

mkdir ../bin/dist/bootstrap/bin | out-null
mkdir ../bin/dist/bootstrap/etc | out-null
mkdir ../bin/dist/bootstrap/global/profiles | out-null
mkdir ../bin/dist/bootstrap/global/sys-apps/fusion | out-null

cp ../bin/Release/bootstrap.exe ../bin/dist/bootstrap/

cp ./bootstrap/config.ini ../bin/dist/bootstrap/etc/
cp ./etc/fusion.s3db ../bin/dist/bootstrap/etc/

cp ../bin/Release/fuse.exe ../bin/dist/bootstrap/bin/
cp ../bin/Release/fuse.exe.config ../bin/dist/bootstrap/bin/
cp ../bin/Release/Fusion.tasks ../bin/dist/bootstrap/bin/
cp ../bin/Release/libfusion.dll ../bin/dist/bootstrap/bin/
cp ../bin/Release/libfusiontasks.dll ../bin/dist/bootstrap/bin/
cp ../bin/Release/sandbox.exe ../bin/dist/bootstrap/bin/
cp ../bin/Release/sandbox.exe.config ../bin/dist/bootstrap/bin/

cp ../lib/log4net.dll ../bin/dist/bootstrap/bin/
cp ../lib/ngetopt.dll ../bin/dist/bootstrap/bin/
cp ../lib/Nini.dll ../bin/dist/bootstrap/bin/
cp ../lib/System.Data.SQLite.dll ../bin/dist/bootstrap/bin/
cp ../lib/System.Data.SQLite.Linq.dll ../bin/dist/bootstrap/bin/

mkdir ../bin/dist/bootstrap/bin/ExtensionPack | out-null
cp ./ExtensionPack/* ../bin/dist/bootstrap/bin/ExtensionPack/
attrib -R ../bin/dist/bootstrap/bin/ExtensionPack/*

cp ./bootstrap/categories ../bin/dist/bootstrap/global/profiles/

cp $port ../bin/dist/bootstrap/global/sys-apps/fusion/

rm ../bin/dist/fusionsetup.exe -ErrorAction SilentlyContinue
./7za.exe a -r -t7z ../bin/dist/bootstrap/archive.7z ../bin/dist/bootstrap/*
gc ./bootstrap/7zS.sfx,./bootstrap/config.txt,../bin/dist/bootstrap/archive.7z -Enc Byte -Read 512 | 
    sc ../bin/dist/fusionsetup.exe -Enc Byte