# Generates the msitasks distribution archive.

if (!($args.Length -eq 1)) {
	write-host "Usage: mkmsitasks.ps1 [ version ]`n"
	return;
}

$ver = $args[0]

if (test-path ../bin/dist/msitasks) {
	rm ../bin/dist/msitasks -recurse -force
}

write-host "Building distribution archive..." -ForegroundColor green

mkdir ../bin/dist/msitasks | out-null
cp ../bin/Release/libmsitasks.dll ../bin/dist/msitasks/
cp ../bin/Release/msi.tasks ../bin/dist/msitasks/

rm ../bin/dist/msitasks-$ver.zip -ErrorAction SilentlyContinue
./7za.exe a -r -tzip ../bin/dist/msitasks-$ver.zip ../bin/dist/msitasks/*