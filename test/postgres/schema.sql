-- Test schema for event-sorcerer historian
CREATE SCHEMA IF NOT EXISTS measuring;

CREATE TABLE IF NOT EXISTS measuring.load_measurement (
    ts              TIMESTAMPTZ     NOT NULL,
    hostname        TEXT            NOT NULL,
    last_1_minute   DOUBLE PRECISION NOT NULL,
    last_5_minutes  DOUBLE PRECISION NOT NULL,
    last_15_minutes DOUBLE PRECISION NOT NULL
);

CREATE TABLE IF NOT EXISTS measuring.heartbeat_measurement (
    ts       TIMESTAMPTZ NOT NULL,
    hostname TEXT        NOT NULL
);
