BEGIN TRANSACTION

ALTER TABLE prims ADD Material tinyint NOT NULL default 3

COMMIT
