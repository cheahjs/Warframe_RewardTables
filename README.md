Warframe_RewardTables
=====================

Extract reward/drop tables from Warframe memory dumps or unpacked resource files.

How To Use
-----
Syntax: Warframe_RewardTables.exe <path> [extract/dump, default=dump]

If the extract option is specified, it will expect a raw Packages.bin that was unpacked from H.Misc.cache.

This option will dump the reward tables and mod/bp drop tables with labels into csv files.

If the dump option is specified, it will expect a text file from a strings.exe output from a process dump of Warframe.

This option will dump the reward tables with no table labels into csv files.
