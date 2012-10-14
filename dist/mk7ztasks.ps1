# Generates the msitasks distribution archive.

if (!($args.Length -eq 1)) {
	write-host "Usage: mk7ztasks.ps1 [ version ]`n"
	return;
}

$ver = $args[0]

if (test-path ../bin/dist/7ziptasks) {
	rm ../bin/dist/7ziptasks -recurse -force
}

write-host "Building distribution archive..." -ForegroundColor green

mkdir ../bin/dist/7ziptasks | out-null
cp ../bin/Release/lib7ztasks.dll ../bin/dist/7ziptasks/
cp ../bin/Release/7zip.tasks ../bin/dist/7ziptasks/

cp ./7za.exe ../bin/dist/7ziptasks/

rm ../bin/dist/7ziptasks-$ver.zip -ErrorAction SilentlyContinue
./7za.exe a -r -tzip ../bin/dist/7ziptasks-$ver.zip ../bin/dist/7ziptasks/*