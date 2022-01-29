# translate

A simple tool to help with resource translation for iOS and
Android project.

## Installing

```
$ python3 -mvenv env
$ . ./env/bin/activate
$ pip install -r requirements.txt
```

## Using

```
$ translate --ios <path_to_ios_project> --out ios.xls
$ translate --android <path_to_android_project> --out android.xls
```

## What it does

The `translate` tool scans project string resources and generates
Excel file containing resources for which translations to some
languages are missing. For the purpose of this tool a translation
is "missing" if resource had been translated to Polish but no
translation to "some" language exists.
